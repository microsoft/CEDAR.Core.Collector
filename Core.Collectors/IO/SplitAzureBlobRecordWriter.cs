// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Context;
using Microsoft.CloudMine.Core.Collectors.Error;
using Microsoft.CloudMine.Core.Collectors.Telemetry;
using Microsoft.CloudMine.Core.Collectors.Utility;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public class SplitAzureBlobRecordWriter<T> : IRecordWriter where T : FunctionContext
    {
        private const long FileSizeLimit = 1024 * 1024 * 512; // 512 MB.
        private const long RecordSizeLimit = 1024 * 1024 * 4; // 4 MB.

        private readonly string blobRoot;
        private readonly string notificationQueuePrefix;
        private readonly string storageConnectionEnvironmentVariable;

        private CloudBlobContainer outContainer;

        private readonly T functionContext;
        private readonly ContextWriter<T> contextWriter;
        private readonly Dictionary<string, WriterState> writers;
        private bool initialized;

        private string outputPathPrefix;

        protected ITelemetryClient TelemetryClient { get; private set; }

        public ConcurrentDictionary<string, int> RecordStats { get; }

        public SplitAzureBlobRecordWriter(string blobRoot,
                                          string notificationQueuePrefix,
                                          ITelemetryClient telemetryClient,
                                          T functionContext,
                                          ContextWriter<T> contextWriter,
                                          string storageConnectionEnvironmentVariable)
        {
            this.blobRoot = blobRoot;
            this.notificationQueuePrefix = notificationQueuePrefix;
            this.functionContext = functionContext;
            this.contextWriter = contextWriter;
            this.storageConnectionEnvironmentVariable = storageConnectionEnvironmentVariable;

            this.TelemetryClient = telemetryClient;

            this.writers = new Dictionary<string, WriterState>();
            this.initialized = false;

            this.RecordStats = new ConcurrentDictionary<string, int>();
        }

        public IEnumerable<string> OutputPaths => this.GetOutputPaths();

        private IEnumerable<string> GetOutputPaths()
        {
            List<string> result = new List<string>();
            foreach (WriterState writer in this.writers.Values)
            {
                result.AddRange(writer.FinalizedOutputPaths);
            }

            return result;
        }

        public void SetOutputPathPrefix(string outputPathPrefix)
        {
            this.outputPathPrefix = outputPathPrefix;
        }

        protected string OutputPathPrefix => this.GetOutputPathPrefix(this.functionContext.FunctionStartDate);

        protected string GetOutputPathPrefix(DateTime dateTimeUtc)
        {
            return $"{this.outputPathPrefix}/{dateTimeUtc:yyyy/MM/dd/HH.mm.ss}_{this.functionContext.SessionId}";
        }

        private async Task InitializeAsync(RecordContext recordContext)
        {
            this.outContainer = await AzureHelpers.GetStorageContainerAsync(this.blobRoot, this.storageConnectionEnvironmentVariable).ConfigureAwait(false);

            await this.GetOrAddWriterAsync(recordContext).ConfigureAwait(false);

            this.initialized = true;
        }

        private async Task<WriterState> GetOrAddWriterAsync(RecordContext recordContext)
        {
            string recordType = recordContext.RecordType;
            if (!this.writers.TryGetValue(recordType, out WriterState writerState))
            {
                writerState = new WriterState(recordType, this.OutputPathPrefix, this.notificationQueuePrefix, this.storageConnectionEnvironmentVariable, this.outContainer);
                await writerState.InitializeAsync().ConfigureAwait(false);
                this.writers[recordType] = writerState;
            }

            return writerState;
        }

        public async Task WriteRecordAsync(JObject record, RecordContext recordContext)
        {
            if (!this.initialized)
            {
                await this.InitializeAsync(recordContext).ConfigureAwait(false);
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
                recordContext.MetadataAugmented = true;
            }

            // Add WriterSource to Metadata after the other metadata is augmented. This is because this value changes between writers and need to be updated before the record is emitted.
            metadataToken = record.SelectToken("$.Metadata");
            JObject metadataObject = (JObject)metadataToken;
            JToken writerSourceToken = metadataObject.SelectToken("$.WriterSource");
            if (writerSourceToken == null)
            {
                // This is the first time we are adding writer source.
                metadataObject.Add("WriterSource", RecordWriterSource.AzureBlob.ToString());
            }
            else
            {
                // Override the existing value.
                writerSourceToken.Replace(RecordWriterSource.AzureBlob.ToString());
            }

            string content = record.ToString(Formatting.None);
            if (content.Length >= RecordSizeLimit)
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

            WriterState writerState = await this.GetOrAddWriterAsync(recordContext).ConfigureAwait(false);
            await writerState.WriteLineAsync(content).ConfigureAwait(false);
            this.RegisterRecord(recordContext.RecordType);
        }

        private void RegisterRecord(string recordType)
        {
            if (!this.RecordStats.TryGetValue(recordType, out int recordCount))
            {
                recordCount = 0;
            }
            this.RecordStats[recordType] = recordCount + 1;
        }

        public async Task FinalizeAsync()
        {
            if (!this.initialized)
            {
                return;
            }

            foreach (WriterState state in this.writers.Values)
            {
                await state.FinalizeAsync().ConfigureAwait(false);
            }

            this.initialized = false;
        }

        public void Dispose()
        {
            if (!this.initialized)
            {
                TelemetryClient.LogWarning("RecordWriter.Dispose was called before RecordWriter was initialized. Ignoring the call.");
                return;
            }

            foreach (WriterState state in writers.Values)
            {
                state.Dispose();
            }
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

        public Task NewOutputAsync(string outputSuffix, int fileIndex = 0)
        {
            // SplitAzureBlobRecordWriter ignores new output creations based on new outputSuffix (since the split is based on the internal record type).
            return Task.CompletedTask;
        }

        private class WriterState : IDisposable
        {
            protected long SizeInBytes { get; private set; }
            public CloudBlockBlob OutputBlob { get; private set; }
            public List<string> FinalizedOutputPaths { get; private set; }
            private CloudQueue notificationQueue;

            private readonly string recordType;
            private readonly string outputPath;
            private StreamWriter writer;
            private readonly CloudBlobContainer outContainer;
            private readonly string notificationQueuePrefix;
            private readonly string storageConnectionEnvironmentVariable;

            private int fileIndex;
            private bool initialized;

            public WriterState(string recordType, string outputPath, string notificationQueuePrefix, string storageConnectionEnvironmentVariable, CloudBlobContainer outContainer)
            {
                this.recordType = recordType;
                this.outputPath = outputPath;
                this.notificationQueuePrefix = notificationQueuePrefix;
                this.storageConnectionEnvironmentVariable = storageConnectionEnvironmentVariable;
                this.outContainer = outContainer;
                this.FinalizedOutputPaths = new List<string>();

                this.OutputBlob = null;
                this.fileIndex = -1;
                this.writer = null;
                this.initialized = false;
            }

            public async Task InitializeAsync()
            {
                this.fileIndex++;
                string suffix = fileIndex == 0 ? "" : $"_{fileIndex}";
                this.OutputBlob = outContainer.GetBlockBlobReference($"{this.recordType}/{this.outputPath}{suffix}.json");
                CloudBlobStream cloudBlobStream = await this.OutputBlob.OpenWriteAsync().ConfigureAwait(false);
                this.writer = new StreamWriter(cloudBlobStream, Encoding.UTF8);

                // Initialize the notification queue only once since we keep using the same queue even for multiple files.
                if (!this.initialized)
                {
                    string recordTypeSuffix = this.recordType.Split('.').Last().ToLower(); // you cannot use upper case characters in Azure queue names :(
                    string recordTypeHash = HashUtility.ComputeSha512(this.recordType).ToLower();
                    string queueName = $"{this.notificationQueuePrefix}-{recordTypeSuffix}-{recordTypeHash}";
                    if (queueName.Length > 63)
                    {
                        // Azure queue names are limited with 63 characters. Use only the first 63 characters.
                        queueName = queueName.Substring(0, 63);
                    }
                    this.notificationQueue = await AzureHelpers.GetStorageQueueAsync(queueName, this.storageConnectionEnvironmentVariable).ConfigureAwait(false);
                }

                this.initialized = true;
            }

            public async Task WriteLineAsync(string content)
            {
                this.SizeInBytes = this.writer.BaseStream.Position;

                // Check if the current file needs to be rolled over.
                if (this.SizeInBytes > FileSizeLimit)
                {
                    await this.NewOutputAsync().ConfigureAwait(false);
                }

                await this.writer.WriteLineAsync(content).ConfigureAwait(false);
            }

            public async Task NewOutputAsync()
            {
                if (this.initialized)
                {
                    this.writer.Dispose();
                    await this.NotifyFinalizedOutputAsync().ConfigureAwait(false);
                }

                await this.InitializeAsync().ConfigureAwait(false);
            }

            public void Dispose()
            {
                this.writer.Dispose();
            }

            public async Task FinalizeAsync()
            {
                if (!this.initialized)
                {
                    return;
                }

                await this.NotifyFinalizedOutputAsync().ConfigureAwait(false);

                this.initialized = false;
            }

            private async Task NotifyFinalizedOutputAsync()
            {
                string finalizedOutputPath = this.OutputBlob.Name;
                this.FinalizedOutputPaths.Add(finalizedOutputPath);
                await this.notificationQueue.AddMessageAsync(new CloudQueueMessage(finalizedOutputPath)).ConfigureAwait(false);
            }
        }
    }
}
