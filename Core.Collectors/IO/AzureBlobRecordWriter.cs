// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Context;
using Microsoft.CloudMine.Core.Collectors.Telemetry;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Blobs.Specialized;
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
        private readonly string storageConnectionEnvironmentVariable;
        private readonly string notificationQueueConnectionEnvironmentVariable;

        private BlobContainerClient outContainer;
        private BlockBlobClient outputBlob;
        private QueueClient queue;

        public AzureBlobRecordWriter(string blobRoot,
                                     string outputQueueName,
                                     string identifier,
                                     ITelemetryClient telemetryClient,
                                     T functionContext,
                                     ContextWriter<T> contextWriter,
                                     string storageConnectionEnvironmentVariable = "AzureWebJobsStorage",
                                     string notificationQueueConnectionEnvironmentVariable = "AzureWebJobsStorage")
            : base(identifier, telemetryClient, functionContext, contextWriter, RecordSizeLimit, FileSizeLimit, source: RecordWriterSource.AzureBlob)
        {
            this.blobRoot = blobRoot;
            this.outputQueueName = outputQueueName;
            this.storageConnectionEnvironmentVariable = storageConnectionEnvironmentVariable;
            this.notificationQueueConnectionEnvironmentVariable = notificationQueueConnectionEnvironmentVariable;
        }

        protected override async Task InitializeInternalAsync()
        {
            this.queue = string.IsNullOrWhiteSpace(this.notificationQueueConnectionEnvironmentVariable) ? null : await AzureHelpers.GetStorageQueueAsync(this.outputQueueName, this.notificationQueueConnectionEnvironmentVariable).ConfigureAwait(false);
            this.outContainer = await AzureHelpers.GetStorageContainerAsync(this.blobRoot, this.storageConnectionEnvironmentVariable).ConfigureAwait(false);
        }

        protected override async Task<StreamWriter> NewStreamWriterAsync(string suffix)
        {
            this.outputBlob = this.outContainer.GetBlockBlobClient($"{this.OutputPathPrefix}{suffix}.json");
            Stream cloudBlobStream = await this.outputBlob.OpenWriteAsync(true).ConfigureAwait(false);
            return new StreamWriter(cloudBlobStream, Encoding.UTF8);
        }

        protected override async Task NotifyCurrentOutputAsync()
        {
            string notificiationMessage = AzureHelpers.GenerateNotificationMessage(this.outputBlob);
            if (this.queue != null)
            {
                await this.queue.SendMessageAsync(notificiationMessage).ConfigureAwait(false);
            }

            this.AddOutputPath(this.outputBlob.Name);
        }
    }
}
