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

        public static async Task<string> GetBlobContentAsync(string container, string path, string storageAccountNameEnvironmentVariable = "StorageAccountName")
        {
            CloudBlockBlob blob = await GetBlob(container, path, storageAccountNameEnvironmentVariable);
            string content = await blob.DownloadTextAsync(System.Text.Encoding.Unicode, null, new BlobRequestOptions { LocationMode = WindowsAzure.Storage.RetryPolicies.LocationMode.PrimaryThenSecondary}, new OperationContext());
            // Ignore BOM character at the beginning of the file, which can happen due to encoding.
            // '\uFEFF' => BOM
            return content[0] == '\uFEFF' ? content.Substring(1) : content;
        }

        public static async Task<CloudBlockBlob> GetBlob(string container, string path, string storageAccountNameEnvironmentVariable = "StorageAccountName")
        {
            CloudBlobContainer blobContainer = await GetBlobContainer(container, storageAccountNameEnvironmentVariable);
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(path);
            return blob;
        }

        public static async Task WriteToBlob(string container, string path, string content, string storageAccountNameEnvironmentVariable = "StorageAccountName")
        {
            CloudBlockBlob outputBlob = await GetBlob(container, path, storageAccountNameEnvironmentVariable);
            CloudBlobStream cloudBlobStream = await outputBlob.OpenWriteAsync().ConfigureAwait(false);
            using (StreamWriter writer = new StreamWriter(cloudBlobStream, Encoding.UTF8))
            {
                await writer.WriteLineAsync(content).ConfigureAwait(false);
            }
        }

        public static async Task<CloudBlobContainer> GetBlobContainer(string container, string storageAccountNameEnvironmentVariable = "StorageAccountName")
        {
            CloudStorageAccount storageAccount = await StorageAccountHelper.GetStorageAccountUsingMsi(storageAccountNameEnvironmentVariable);
            //CloudBlobClient storageBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobClient storageBlobClient = new CloudBlobClient(storageAccount.BlobStorageUri, storageAccount.Credentials);
            CloudBlobContainer blobContainer = storageBlobClient.GetContainerReference(container);

            //BlobContainerClient containerClient = new BlobContainerClient(new Uri(storageAccount.BlobEndpoint), new DefaultAzureCredential())
            return blobContainer;
        }

        public static async Task<CloudBlobContainer> GetStorageContainerAsync(string container, string storageAccountNameEnvironmentVariable = "StorageAccountName")
        {
            CloudBlobContainer storageContainer = await GetBlobContainer(container, storageAccountNameEnvironmentVariable);
            await storageContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
            return storageContainer;
        }

        public static async Task<CloudQueue> GetStorageQueueCachedAsync(string queueName, string storageAccountNameEnvironmentVariable = "StorageAccountName")
        {
            await CloudResourceLock.WaitAsync().ConfigureAwait(false);
            try
            {
                string key = $"{queueName}:{storageAccountNameEnvironmentVariable}";
                if (!CloudQueues.TryGetValue(key, out CachedCloudQueue cachedCloudQueue) || (DateTime.UtcNow - cachedCloudQueue.LastInitializationDateUtc) >= CloudResourcesInitializitonFrequency)
                {
                    CloudQueue cloudQueue = await GetStorageQueueAsync(queueName, storageAccountNameEnvironmentVariable).ConfigureAwait(false);
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

        public static async Task<CloudQueue> GetStorageQueueAsync(string queueName, string storageAccountNameEnvironmentVariable = "StorageAccountNameEnvironmentVariable")
        {
            CloudStorageAccount storageAccount = await StorageAccountHelper.GetStorageAccountUsingMsi(storageAccountNameEnvironmentVariable, isQueue: true);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(queueName);
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
            return queue;
        }

        public static async Task<List<CloudQueue>> ListStorageQueuesAsync(string prefix, string storageAccountNameEnvironmentVariable = "StorageAccountNameEnvironmentVariable")
        {
            List<CloudQueue> result = new List<CloudQueue>();

            CloudStorageAccount storageAccount = await StorageAccountHelper.GetStorageAccountUsingMsi(storageAccountNameEnvironmentVariable, isQueue: true);
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

        public static async Task<List<CloudQueue>> ListStorageQueuesAsync(string storageAccountNameEnvironmentVariable = "StorageAccountNameEnvironmentVariable")
        {
            List<CloudQueue> result = new List<CloudQueue>();

            CloudStorageAccount storageAccount = await StorageAccountHelper.GetStorageAccountUsingMsi(storageAccountNameEnvironmentVariable, isQueue: true);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            QueueContinuationToken continuationToken = null;
            do
            {
                QueueResultSegment queueResultSegment = await queueClient.ListQueuesSegmentedAsync(continuationToken).ConfigureAwait(false);
                continuationToken = queueResultSegment.ContinuationToken;

                result.AddRange(queueResultSegment.Results);
            } while (continuationToken != null);

            return result;
        }

        public static async Task<CloudTable> GetStorageTableAsync(string tableName, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            CloudStorageAccount storageAccount = StorageAccountHelper.GetStorageAccount(storageConnectionEnvironmentVariable);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(tableName);
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
            string phrase = AzureStorageResourceHelper.GetBlobResource(storageAccountName);
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
