// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Sas;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Azure.Data.Tables;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public static class AzureHelpers
    {
        private static readonly TimeSpan CloudResourcesInitializitonFrequency = TimeSpan.FromMinutes(10);
        private static Dictionary<string, CachedCloudQueue> CloudQueues = new Dictionary<string, CachedCloudQueue>();
        private static readonly System.Threading.SemaphoreSlim CloudResourceLock = new System.Threading.SemaphoreSlim(1, 1);

        public class CachedCloudQueue
        {
            public QueueClient CloudQueue { get; }
            public DateTime LastInitializationDateUtc { get; }

            public CachedCloudQueue(QueueClient cloudQueue, DateTime lastInitializationDateUtc)
            {
                this.CloudQueue = cloudQueue;
                this.LastInitializationDateUtc = lastInitializationDateUtc;
            }
        }

        private static string GetStorageAccount(string storageConnectionEnvironmentVariable)
        {
            string stagingBlobConnectionString = Environment.GetEnvironmentVariable(storageConnectionEnvironmentVariable);
            return stagingBlobConnectionString;
        }

        public static async Task<string> GetBlobContentAsync(string container, string path, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            BlockBlobClient blob = GetBlob(container, path, storageConnectionEnvironmentVariable);
            string content = (await blob.DownloadContentAsync()).Value.Content.ToString();
            // Ignore BOM character at the beginning of the file, which can happen due to encoding.
            // '\uFEFF' => BOM
            return content[0] == '\uFEFF' ? content.Substring(1) : content;
        }

        public static BlockBlobClient GetBlob(string container, string path, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            BlobContainerClient blobContainer = GetBlobContainer(container, storageConnectionEnvironmentVariable);
            BlockBlobClient blob = blobContainer.GetBlockBlobClient(path);
            return blob;
        }

        public static async Task WriteToBlob(string container, string path, string content)
        {
            BlockBlobClient outputBlob = GetBlob(container, path);
            Stream cloudBlobStream = await outputBlob.OpenWriteAsync(true).ConfigureAwait(false);
            using (StreamWriter writer = new StreamWriter(cloudBlobStream, Encoding.UTF8))
            {
                await writer.WriteLineAsync(content).ConfigureAwait(false);
            }
        }
        
        public static BlobContainerClient GetBlobContainer(string container, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            string connectionString = GetStorageAccount(storageConnectionEnvironmentVariable);
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient blobContainer = blobServiceClient.GetBlobContainerClient(container);
            return blobContainer;
        }

        public static async Task<BlobContainerClient> GetStorageContainerAsync(string container, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            BlobContainerClient storageContainer = GetBlobContainer(container, storageConnectionEnvironmentVariable);
            await storageContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
            return storageContainer;
        }

        public static async Task<QueueClient> GetStorageQueueCachedAsync(string queueName, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            await CloudResourceLock.WaitAsync().ConfigureAwait(false);
            try
            {
                string key = $"{queueName}:{storageConnectionEnvironmentVariable}";
                if (!CloudQueues.TryGetValue(key, out CachedCloudQueue cachedCloudQueue) || (DateTime.UtcNow - cachedCloudQueue.LastInitializationDateUtc) >= CloudResourcesInitializitonFrequency)
                {
                    QueueClient cloudQueue = await GetStorageQueueAsync(queueName, storageConnectionEnvironmentVariable).ConfigureAwait(false);
                    cachedCloudQueue = new CachedCloudQueue(cloudQueue, DateTime.UtcNow);
                    CloudQueues[key] = cachedCloudQueue;
                }

                return cachedCloudQueue.CloudQueue;
            }
            finally
            {
                CloudResourceLock.Release();
            }
        }

        public static async Task<QueueClient> GetStorageQueueAsync(string queueName, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            string connectionString = GetStorageAccount(storageConnectionEnvironmentVariable);
            QueueClient queue = new QueueClient(connectionString, queueName);
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
            return queue;
        }

        public static async Task<List<QueueClient>> ListStorageQueuesAsync(string prefix, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            List<QueueItem> result = new List<QueueItem>();

            string connectionString = GetStorageAccount(storageConnectionEnvironmentVariable);
            QueueServiceClient queueServiceClient = new QueueServiceClient(connectionString);
            await foreach (Page<QueueItem> page in queueServiceClient.GetQueuesAsync(prefix: prefix).AsPages()) {
                result.AddRange(page.Values);
            }
            List<QueueClient> clientResult = new List<QueueClient>();
            foreach (QueueItem queueItem in result)
            {
                QueueClient queueClient = new QueueClient(connectionString, queueItem.Name);
                clientResult.Add(queueClient);
            }

            return clientResult;
        }

        public static async Task<List<QueueClient>> ListStorageQueuesAsync(string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            List<QueueItem> result = new List<QueueItem>();
            string connectionString = GetStorageAccount(storageConnectionEnvironmentVariable);
            QueueServiceClient queueServiceClient = new QueueServiceClient(connectionString);
            await foreach (Page<QueueItem> page in queueServiceClient.GetQueuesAsync().AsPages())
            {
                result.AddRange(page.Values);
            }
            List<QueueClient> clientResult = new List<QueueClient>();
            foreach (QueueItem queueItem in result)
            {
                QueueClient queueClient = new QueueClient(connectionString, queueItem.Name);
                clientResult.Add(queueClient);
            }

            return clientResult;
        }

        public static async Task<TableClient> GetStorageTableAsync(string tableName, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            string connectionString = GetStorageAccount(storageConnectionEnvironmentVariable);
            TableServiceClient tableClient = new TableServiceClient(connectionString);
            TableClient table = tableClient.GetTableClient(tableName);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);
            return table;
        }

        public static string GenerateNotificationMessage(BlobBaseClient blob)
        {

            BlobSasBuilder blobSasBuilder = new BlobSasBuilder(permissions: BlobContainerSasPermissions.Read | BlobContainerSasPermissions.List, expiresOn: DateTimeOffset.UtcNow.AddDays(7))
            {
                StartsOn = DateTimeOffset.UtcNow
            };

            Uri sasUri = blob.GenerateSasUri(blobSasBuilder);
            string blobUriSas = sasUri.ToString();
            string notificiationMessage = $"AzureBlob: 1.0\n{{\"BlobRecords\": [{{\"Path\": \"{blobUriSas}\"}}]}}";
            return notificiationMessage;
        }

        public static Tuple<string, string> GetContainerAndRelativePathFromMessage(string notificationMessage, string storageAccountName)
        {
            string phrase = $"https://{storageAccountName}.blob.core.windows.net/";
            if (!notificationMessage.Contains(phrase))
            {
                return null;
            }

            string blobPath = notificationMessage.Split(phrase).Last().Split('?').First();
            if (string.IsNullOrWhiteSpace(blobPath))
            {
                return null;
            }

            string[] parts = blobPath.Split('/');
            string container = parts[0];
            string path = blobPath.Split($"{container}/").Last();
            return Tuple.Create(container, path);
        }
    }
}
