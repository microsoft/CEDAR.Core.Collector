// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Context;
using Microsoft.CloudMine.Core.Telemetry;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public class AzureBlobRecordWriter<T> : RecordWriterCore<T> where T : FunctionContext
    {
        private const long FileSizeLimit = 1024 * 1024 * 512; // 512 MB.
        private const long RecordSizeLimit = 1024 * 1024 * 4; // 4 MB.

        private readonly string blobRoot;
        private readonly string outputQueueName;
        private readonly string storageAccountEnvironmentVariable;
        private readonly string notificationQueueConnectionEnvironmentVariable;

        private CloudBlobContainer outContainer;
        private CloudBlockBlob outputBlob;
        private CloudQueue queue;

        public AzureBlobRecordWriter(string blobRoot,
                                     string outputQueueName,
                                     string identifier,
                                     ITelemetryClient telemetryClient,
                                     T functionContext,
                                     ContextWriter<T> contextWriter,
                                     string storageAccountEnvironmentVariable = "StorageAccountName",
                                     string notificationQueueConnectionEnvironmentVariable = "AzureWebJobsStorage")
            : base(identifier, telemetryClient, functionContext, contextWriter, RecordSizeLimit, FileSizeLimit, source: RecordWriterSource.AzureBlob)
        {
            this.blobRoot = blobRoot;
            this.outputQueueName = outputQueueName;
            this.storageAccountEnvironmentVariable = storageAccountEnvironmentVariable;
            this.notificationQueueConnectionEnvironmentVariable = notificationQueueConnectionEnvironmentVariable;
        }

        protected override async Task InitializeInternalAsync()
        {
            this.queue = string.IsNullOrWhiteSpace(this.notificationQueueConnectionEnvironmentVariable) ? null : await AzureHelpers.GetStorageQueueAsync(this.outputQueueName, this.notificationQueueConnectionEnvironmentVariable).ConfigureAwait(false);
            this.outContainer = await AzureHelpers.GetStorageContainerUsingMsiAsync(this.blobRoot, this.storageAccountEnvironmentVariable).ConfigureAwait(false);
        }

        protected override async Task<StreamWriter> NewStreamWriterAsync(string suffix)
        {
            this.outputBlob = this.outContainer.GetBlockBlobReference($"{this.OutputPathPrefix}{suffix}.json");
            CloudBlobStream cloudBlobStream = await this.outputBlob.OpenWriteAsync().ConfigureAwait(false);
            return new StreamWriter(cloudBlobStream, Encoding.UTF8);
        }

        protected override async Task NotifyCurrentOutputAsync()
        {
            string notificiationMessage = AzureHelpers.GenerateNotificationMessage(this.outputBlob);
            if (this.queue != null)
            {
                await this.queue.AddMessageAsync(new CloudQueueMessage(notificiationMessage)).ConfigureAwait(false);
            }

            this.AddOutputPath(this.outputBlob.Name);
        }

        public override void AddFilePath(string filePath)
        {
            // No file mapping is done in azure blob writer, so ignore.
        }
    }
}
