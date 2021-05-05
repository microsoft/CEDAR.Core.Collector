// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Context;
using Microsoft.CloudMine.Core.Collectors.Telemetry;
using Microsoft.Azure.DataLake.Store;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.DataLake.Store.FileTransfer;
using Microsoft.CloudMine.Core.Collectors.Error;
using System.Diagnostics;
using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public class AdlsConfig
    {
        public AdlsClient AdlsClient { get; }
        public string AdlsRoot { get; }
        public string Version { get; }

        public AdlsConfig(AdlsClient adlsClient, string adlsRoot, string version)
        {
            this.AdlsClient = adlsClient;
            this.AdlsRoot = adlsRoot;
            this.Version = version;
        }
    }

    public class AdlsBulkRecordWriter<T> : RecordWriterCore<T> where T : FunctionContext
    {
        private const long FileSizeLimit = 1024 * 1024 * 512; // 512 MB.
        private const long RecordSizeLimit = 1024 * 1024 * 4; // 4 MB.

        private readonly static TimeSpan MaxUploadDelay = TimeSpan.FromMinutes(10);

        private readonly string uniqueId;
        private readonly List<AdlsConfig> adlsConfigs;

        private string localRoot;

        private string currentSuffix;
        private string currentLocalPath;

        // Keeping this constructor for backwards compatibility for now.
        public AdlsBulkRecordWriter(AdlsClient adlsClient,
                                    string identifier,
                                    ITelemetryClient telemetryClient,
                                    T functionContext,
                                    ContextWriter<T> contextWriter,
                                    string root,
                                    string version)
            : this(adlsConfigs: new List<AdlsConfig>() { new AdlsConfig(adlsClient, root, version) }, identifier, telemetryClient, functionContext, contextWriter)
        {
        }

        public AdlsBulkRecordWriter(List<AdlsConfig> adlsConfigs,
                                    string identifier,
                                    ITelemetryClient telemetryClient,
                                    T functionContext,
                                    ContextWriter<T> contextWriter)
            : base(identifier, telemetryClient, functionContext, contextWriter, RecordSizeLimit, FileSizeLimit, source: RecordWriterSource.AzureDataLake)
        {
            this.adlsConfigs = adlsConfigs;
            this.uniqueId = functionContext.SessionId;
            this.currentSuffix = null;
        }

        protected override Task InitializeInternalAsync()
        {
            this.localRoot = Path.Combine(Path.GetTempPath(), this.uniqueId);
            return Task.CompletedTask;
        }

        protected override Task<StreamWriter> NewStreamWriterAsync(string suffix)
        {
            this.currentSuffix = suffix;

            string fileName = $"{this.OutputPathPrefix}{this.currentSuffix}.json";
            this.currentLocalPath = Path.Combine(this.localRoot, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(this.currentLocalPath));

            StreamWriter result = File.CreateText(this.currentLocalPath);
            return Task.FromResult(result);
        }

        protected override async Task NotifyCurrentOutputAsync()
        {
            if (this.adlsConfigs.Count == 0)
            {
                throw new FatalTerminalException("No ADLS Config found for upload.");
            }

            // Assume that upload will take at most 10 minutes.
            DateTime dateTimeSignature = DateTime.UtcNow + MaxUploadDelay;
            string fileName = $"{this.GetOutputPathPrefix(dateTimeSignature)}{this.currentSuffix}.json";
            string finalOutputPath = Path.Combine(this.localRoot, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(finalOutputPath));

            // Rename local file.
            try
            {
                File.Move(this.currentLocalPath, finalOutputPath);
            }
            catch (Exception)
            {
                // Retry once, just in case.
                try
                {
                    File.Move(this.currentLocalPath, finalOutputPath);
                }
                catch (Exception exception)
                {
                    string message = $"ADLS Bulk Record Writer: cannot move file '{currentLocalPath}' to '{finalOutputPath}'.";
                    this.TelemetryClient.TrackException(exception, message);
                    throw new FatalException(message);
                }
            }

            List<Task<string>> uploadTasks = new List<Task<string>>();
            foreach (AdlsConfig adlsConfig in this.adlsConfigs)
            {
                Task<string> uploadTask = Task<string>.Factory.StartNew(() => BulkUploadToAdlsConfig(finalOutputPath, adlsConfig));
                uploadTasks.Add(uploadTask);
            }

            try
            {
                IEnumerable<string> adlsDirectories = await Task.WhenAll<string>(uploadTasks).ConfigureAwait(false);
                foreach (string adlsDirectory in adlsDirectories)
                {
                    string finalAdlsOutputPath = finalOutputPath.Replace($"{this.localRoot}\\", $"{adlsDirectory}/");
                    this.AddOutputPath(finalAdlsOutputPath);
                }
            }
            catch (Exception exception)
            {
                this.TelemetryClient.TrackException(exception, "ADLS Bulk Record Writer: upload failed.");
            }
            finally
            {
                try
                {
                    File.Delete(finalOutputPath);
                }
                catch (Exception)
                {
                    // Retry once, just in case.
                    try
                    {
                        File.Delete(finalOutputPath);
                    }
                    catch (Exception exception)
                    {
                        string message = $"ADLS Bulk Record Writer: cannot delete file '{finalOutputPath}' after upload.";
                        this.TelemetryClient.TrackException(exception, message);
                        throw new FatalException(message);
                    }
                }
            }
        }

        private string BulkUploadToAdlsConfig(string finalOutputPath, AdlsConfig adlsConfig)
        {
            Stopwatch uploadTimer = Stopwatch.StartNew();
            string adlsDirectory = $"{adlsConfig.AdlsRoot}/{adlsConfig.Version}";

            TransferStatus status = adlsConfig.AdlsClient.BulkUpload(this.localRoot, adlsDirectory);
            bool retried = false;
            if (status.EntriesFailed.Count != 0)
            {
                retried = true;
                // Retry once.
                status = adlsConfig.AdlsClient.BulkUpload(this.localRoot, adlsDirectory, shouldOverwrite: IfExists.Fail);
                if (status.EntriesFailed.Count != 0)
                {
                    foreach (SingleEntryTransferStatus failedTransferStatus in status.EntriesFailed)
                    {
                        Dictionary<string, string> transferStatusProperties = new Dictionary<string, string>()
                        {
                            { "EntryName", failedTransferStatus.EntryName },
                            { "EntrySize", failedTransferStatus.EntrySize.ToString() },
                            { "TransferErrors", failedTransferStatus.Errors },
                            { "TransferStatus", failedTransferStatus.Status.ToString() },
                            { "TransferType", failedTransferStatus.Type.ToString() },
                        };
                        this.TelemetryClient.TrackEvent("FailedTransferStatus", transferStatusProperties);
                    }
                    throw new FatalException($"Cannot bulk upload '{finalOutputPath}'.");
                }
            }

            uploadTimer.Stop();
            TimeSpan uploadDuration = uploadTimer.Elapsed;

            Dictionary<string, string> properties = new Dictionary<string, string>()
            {
                { "Duration", uploadDuration.ToString() },
                { "Retried", retried.ToString() },
                { "SizeBytes", this.SizeInBytes.ToString() },
                { "LocalPath", finalOutputPath },
            };
            this.TelemetryClient.TrackEvent("AdlsUploadStats", properties);

            return adlsDirectory;
        }

        public override async Task FinalizeAsync()
        {
            await base.FinalizeAsync().ConfigureAwait(false);

            try
            {
                Directory.Delete(this.localRoot, true);
            }
            catch (Exception)
            {
                // Retry once, just in case.
                try
                {
                    Directory.Delete(this.localRoot, true);
                }
                catch (Exception exception)
                {
                    this.TelemetryClient.TrackException(exception, "Cannot delete session root.");
                }
            }
        }
    }
}
