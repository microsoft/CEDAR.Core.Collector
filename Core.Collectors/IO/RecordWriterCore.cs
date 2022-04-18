// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Context;
using Microsoft.CloudMine.Core.Collectors.Error;
using Microsoft.CloudMine.Core.Telemetry;
using Microsoft.CloudMine.Core.Collectors.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public abstract class RecordWriterCore<T> : IRecordWriter where T : FunctionContext
    {
        private readonly string identifier;
        private readonly T functionContext;
        private readonly ContextWriter<T> contextWriter;
        private readonly long recordSizeLimit;
        private readonly long fileSizeLimit;
        private readonly List<string> outputPaths;
        private readonly RecordWriterSource source;

        private string outputPathPrefix;
        private bool initialized;

        private StreamWriter currentWriter;
        private string currentOutputSuffix;
        private int currentFileIndex;

        // Used to detect threading issues; RecordWriter is not thread-safe
        private int isActive;

        protected ITelemetryClient TelemetryClient { get; private set; }

        protected long SizeInBytes { get; private set; }

        public ConcurrentDictionary<string, int> RecordStats { get; }

        protected RecordWriterCore(string identifier,
                                   ITelemetryClient telemetryClient,
                                   T functionContext,
                                   ContextWriter<T> contextWriter,
                                   long recordSizeLimit,
                                   long fileSizeLimit,
                                   RecordWriterSource source)
        {
            this.identifier = identifier;
            this.TelemetryClient = telemetryClient;
            this.functionContext = functionContext;
            this.contextWriter = contextWriter;
            this.recordSizeLimit = recordSizeLimit;
            this.fileSizeLimit = fileSizeLimit;
            this.outputPaths = new List<string>();

            this.currentFileIndex = 0;
            this.source = source;

            this.RecordStats = new ConcurrentDictionary<string, int>();
        }

        protected abstract Task InitializeInternalAsync();
        protected abstract Task<StreamWriter> NewStreamWriterAsync(string suffix);
        protected abstract Task NotifyCurrentOutputAsync();

        public IEnumerable<string> OutputPaths => this.outputPaths;

        protected void AddOutputPath(string outputPath)
        {
            this.outputPaths.Add(outputPath);
        }

        public void SetOutputPathPrefix(string outputPathPrefix)
        {
            this.outputPathPrefix = outputPathPrefix;
        }

        protected string OutputPathPrefix => this.GetOutputPathPrefix(this.functionContext.FunctionStartDate);

        protected string GetOutputPathPrefix(DateTime dateTimeUtc)
        {
            return $"{this.outputPathPrefix}/{dateTimeUtc:yyyy/MM/dd/HH.mm.ss}_{this.identifier}_{this.functionContext.SessionId}";
        }

        private async Task InitializeAsync(string outputSuffix)
        {
            if (this.outputPathPrefix == null)
            {
                string message = "Cannot initialize record writer before the context is set.";
                this.TelemetryClient.LogCritical(message);
                throw new FatalTerminalException(message);
            }

            await this.InitializeInternalAsync().ConfigureAwait(false);
            this.currentWriter = await this.NewStreamWriterAsync(outputSuffix).ConfigureAwait(false);
            this.currentOutputSuffix = outputSuffix;

            this.initialized = true;
        }

        public async Task NewOutputAsync(string outputSuffix, int fileIndex = 0)
        {
            if (this.initialized)
            {
                this.currentWriter.Dispose();
                try
                {
                    // There is a chance that NotifyCurrentOutputAsync throws. Even if this is the case, we want to ensure that a new writer is initialized (using NewStreamWriterAsync(..)) so that the recordWriter
                    // is still in a 'valid' state (e.g., the client can call WriteLineAsync(..)). Without this, since the currentWriter is disposed on the line before, the recordWriter starts throwing an ObjectDisposedException
                    // with message: "Cannot access a closed file."
                    await this.NotifyCurrentOutputAsync().ConfigureAwait(false);
                }
                finally
                {
                    string finalSuffix = $"{(string.IsNullOrWhiteSpace(outputSuffix) ? "" : $"_{outputSuffix}")}{(fileIndex == 0 ? "" : $"_{fileIndex}")}";
                    this.currentWriter = await this.NewStreamWriterAsync(finalSuffix).ConfigureAwait(false);
                    this.currentOutputSuffix = outputSuffix;
                }
            }
            else
            {
                await this.InitializeAsync($"_{outputSuffix}").ConfigureAwait(false);
            }
        }

        public async Task WriteLineAsync(string content)
        {
            if (!this.initialized)
            {
                await this.InitializeAsync(outputSuffix: string.Empty).ConfigureAwait(false);
            }

            this.SizeInBytes = this.currentWriter.BaseStream.Position;

            // Check if the current file needs to be rolled over.
            if (this.SizeInBytes > this.fileSizeLimit)
            {
                this.currentFileIndex++;
                await this.NewOutputAsync(this.currentOutputSuffix, this.currentFileIndex).ConfigureAwait(false);
            }

            await this.currentWriter.WriteLineAsync(content).ConfigureAwait(false);
        }

        public async Task WriteRecordAsync(JObject record, RecordContext recordContext)
        {
            if (Interlocked.Exchange(ref isActive, 1) != 0)
            {
                throw new FatalTerminalException("Collectors are not allowed to output in parallel. This is a bug in the collector!");
            }

            try
            {
                if (!this.initialized)
                {
                    await this.InitializeAsync(outputSuffix: string.Empty).ConfigureAwait(false);
                }

                // Augment the metadata to the record only if not done by another record writer.
                JToken metadataToken = record.SelectToken("$.Metadata");
                if (recordContext.MetadataAugmented)
                {
                    // Confirm (double check) that this is case and fail execution if not.
                    if (metadataToken == null)
                    {
                        Dictionary<string, string> properties = new Dictionary<string, string>()
                        {
                            { "RecordType", recordContext.RecordType },
                            { "RecordMetadata", record.SelectToken("$.Metadata").ToString(Formatting.None) },
                            { "RecordPrefix", record.ToString(Formatting.None).Substring(0, 1024) },
                        };
                        this.TelemetryClient.TrackEvent("RecordWithoutMetadata", properties);

                        throw new FatalTerminalException("Detected a record without metadata. Investigate 'RecordWithoutMetadata' custom event for details.");
                    }
                }
                else
                {
                    this.AugmentRecordMetadata(record, recordContext);
                    this.AugmentRecord(record);

                    recordContext.MetadataAugmented = true;
                }

                // Add WriterSource to Metadata after the other metadata is augmented.
                // This value changes between writers and needs to be updated before the record is emitted.
                metadataToken = record.SelectToken("$.Metadata");
                JObject metadataObject = (JObject)metadataToken;
                JToken writerSourceToken = metadataObject.SelectToken("$.WriterSource");
                if (writerSourceToken == null)
                {
                    // This is the first time we are adding writer source.
                    metadataObject.Add("WriterSource", this.source.ToString());
                }
                else
                {
                    // Override the existing value.
                    writerSourceToken.Replace(this.source.ToString());
                }

                string content = record.ToString(Formatting.None);
                // +2 for CR+LF
                if (Encoding.UTF8.GetMaxByteCount(content.Length) + 2 >= this.recordSizeLimit &&
                    Encoding.UTF8.GetByteCount(content) + 2 >= this.recordSizeLimit)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>()
                    {
                        { "RecordType", recordContext.RecordType },
                        { "RecordMetadata", record.SelectToken("$.Metadata").ToString(Formatting.None) },
                        { "RecordPrefix", content.Substring(0, 1024) },
                    };

                    this.TelemetryClient.TrackEvent("DroppedRecord", properties);
                    return;
                }

                await this.WriteLineAsync(content).ConfigureAwait(false);

                this.RegisterRecord(recordContext.RecordType);
            }
            finally
            {
                Interlocked.Exchange(ref isActive, 0);
            }
        }

        private void RegisterRecord(string recordType)
        {
            if (!this.RecordStats.TryGetValue(recordType, out int recordCount))
            {
                recordCount = 0;
            }
            this.RecordStats[recordType] = recordCount + 1;
        }

        protected virtual void AugmentRecord(JObject record)
        {
            // Default implementation does not do anything.
        }

        public virtual async Task FinalizeAsync()
        {
            if (!this.initialized)
            {
                return;
            }

            await this.NotifyCurrentOutputAsync().ConfigureAwait(false);

            this.initialized = false;
        }

        public void Dispose()
        {
            if (!this.initialized)
            {
                return;
            }

            this.currentWriter.Dispose();
        }

        private void AugmentRecordMetadata(JObject record, RecordContext recordContext)
        {
            string serializedRecord = record.ToString(Formatting.None);

            JToken metadataToken = record.SelectToken("Metadata");
            if (metadataToken == null)
            {
                metadataToken = new JObject();
                record.AddFirst(new JProperty("Metadata", metadataToken));
            }

            JObject metadata = (JObject)metadataToken;

            this.contextWriter.AugmentMetadata(metadata, this.functionContext);

            metadata.Add("RecordType", recordContext.RecordType);
            metadata.Add("CollectionDate", DateTime.UtcNow);

            Dictionary<string, JToken> additionalMetadata = recordContext.AdditionalMetadata;
            if (additionalMetadata != null)
            {
                foreach (KeyValuePair<string, JToken> metadataItem in additionalMetadata)
                {
                    metadata.Add(metadataItem.Key, metadataItem.Value);
                }
            }

            metadata.Add("RecordSha", HashUtility.ComputeSha512(serializedRecord));
        }
    }

    public static class RecordWriterExtensions
    {
        public static string GetOutputPaths(List<IRecordWriter> recordWriters)
        {
            string result = string.Join(";", recordWriters.Select(recordWriter => string.Join(";", recordWriter.OutputPaths)));
            return result;
        }

        public static string GetOutputPaths(IRecordWriter recordWriter)
        {
            return GetOutputPaths(new List<IRecordWriter> { recordWriter });
        }
    }

    public enum RecordWriterSource
    {
        AzureBlob = 0,
        AzureDataLake = 1,
    }
}
