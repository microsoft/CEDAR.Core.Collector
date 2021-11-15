// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            public CloudQueue CloudQueue { get; }
            public DateTime LastInitializationDateUtc { get; }

            public CachedCloudQueue(CloudQueue cloudQueue, DateTime lastInitializationDateUtc)
            {
                this.CloudQueue = cloudQueue;
                this.LastInitializationDateUtc = lastInitializationDateUtc;
            }
        }

        private static CloudStorageAccount GetStorageAccount(string storageConnectionEnvironmentVariable)
        {
            string stagingBlobConnectionString = Environment.GetEnvironmentVariable(storageConnectionEnvironmentVariable);
            return CloudStorageAccount.Parse(stagingBlobConnectionString);
        }

        public static async Task<string> GetBlobContentAsync(string container, string path, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            CloudBlockBlob blob = GetBlob(container, path, storageConnectionEnvironmentVariable);
            string content = await blob.DownloadTextAsync();
            // Ignore BOM character at the beginning of the file, which can happen due to encoding.
            // '\uFEFF' => BOM
            return content[0] == '\uFEFF' ? content.Substring(1) : content;
        }

        public static CloudBlockBlob GetBlob(string container, string path, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            CloudBlobContainer blobContainer = GetBlobContainer(container, storageConnectionEnvironmentVariable);
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(path);
            return blob;
        }

        public static async Task WriteToBlob(string container, string path, string content)
        {
            CloudBlockBlob outputBlob = GetBlob(container, path);
            CloudBlobStream cloudBlobStream = await outputBlob.OpenWriteAsync().ConfigureAwait(false);
            using (StreamWriter writer = new StreamWriter(cloudBlobStream, Encoding.UTF8))
            {
                await writer.WriteLineAsync(content).ConfigureAwait(false);
            }
        }

        public static CloudBlobContainer GetBlobContainer(string container, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            CloudStorageAccount storageAccount = GetStorageAccount(storageConnectionEnvironmentVariable);
            CloudBlobClient storageBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = storageBlobClient.GetContainerReference(container);
            return blobContainer;
        }

        public static async Task<CloudBlobContainer> GetStorageContainerAsync(string container, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            CloudBlobContainer storageContainer = GetBlobContainer(container, storageConnectionEnvironmentVariable);
            await storageContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
            return storageContainer;
        }

        public static async Task<CloudQueue> GetStorageQueueCachedAsync(string queueName, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            await CloudResourceLock.WaitAsync().ConfigureAwait(false);
            try
            {
                string key = $"{queueName}:{storageConnectionEnvironmentVariable}";
                if (!CloudQueues.TryGetValue(key, out CachedCloudQueue cachedCloudQueue) || (DateTime.UtcNow - cachedCloudQueue.LastInitializationDateUtc) >= CloudResourcesInitializitonFrequency)
                {
                    CloudQueue cloudQueue = await GetStorageQueueAsync(queueName, storageConnectionEnvironmentVariable).ConfigureAwait(false);
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

        public static async Task<CloudQueue> GetStorageQueueAsync(string queueName, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            CloudStorageAccount storageAccount = GetStorageAccount(storageConnectionEnvironmentVariable);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(queueName);
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
            return queue;
        }

        public static async Task<List<CloudQueue>> ListStorageQueuesAsync(string prefix, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            List<CloudQueue> result = new List<CloudQueue>();

            CloudStorageAccount storageAccount = GetStorageAccount(storageConnectionEnvironmentVariable);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            QueueContinuationToken continuationToken = null;
            do
            {
                QueueResultSegment queueResultSegment = await queueClient.ListQueuesSegmentedAsync(prefix, continuationToken).ConfigureAwait(false);
                continuationToken = queueResultSegment.ContinuationToken;

                result.AddRange(queueResultSegment.Results);
            } while (continuationToken != null);

            return result;
        }

        public static async Task<CloudTable> GetStorageTableAsync(string tableName, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            CloudStorageAccount storageAccount = GetStorageAccount(storageConnectionEnvironmentVariable);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);
            return table;
        }

        public static async Task<TableClient> GetTableClientAsync(string tableName, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            string connectionString = Environment.GetEnvironmentVariable(storageConnectionEnvironmentVariable);
            TableClient table = new TableClient(connectionString, tableName);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);
            return table;
        }

        public static string GenerateNotificationMessage(CloudBlob blob)
        {
            string blobSas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List,
                SharedAccessStartTime = DateTimeOffset.UtcNow,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddDays(7)
            });
            string blobUriSas = string.Concat(blob.Uri, blobSas);
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
