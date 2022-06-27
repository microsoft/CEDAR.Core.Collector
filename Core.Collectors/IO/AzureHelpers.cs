// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.CloudMine.Core.Telemetry;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
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
        private static readonly string EndPointSuffix = "core.windows.net";
        private static DateTime TokenExpiration = DateTime.MinValue;

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

        public static async Task<string> GetBlobContentAsync(string container, string path, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            CloudBlockBlob blob = GetBlob(container, path, storageConnectionEnvironmentVariable);
            string content = await blob.DownloadTextAsync();
            // Ignore BOM character at the beginning of the file, which can happen due to encoding.
            // '\uFEFF' => BOM
            return content[0] == '\uFEFF' ? content.Substring(1) : content;
        }

        public static async Task<string> GetBlobContentUsingMsiAsync(string container, string path, string storageAccountNameEnvironmentVariable, ITelemetryClient telemetryClient = null)
        {
            CloudBlockBlob blob = await GetBlobUsingMsiAsync(container, path, storageAccountNameEnvironmentVariable, telemetryClient).ConfigureAwait(false);
            string content = await blob.DownloadTextAsync();
            // Ignore BOM character at the beginning of the file, which can happen due to encoding.
            // '\uFEFF' => BOM
            return content[0] == '\uFEFF' ? content.Substring(1) : content;
        }

        public static async Task<CloudBlockBlob> GetBlobUsingMsiAsync(string container, string path, string storageAccountNameEnvironmentVariable, ITelemetryClient telemetryClient = null)
        {
            CloudBlobContainer blobContainer = await GetBlobContainerUsingMsiAsync(container, storageAccountNameEnvironmentVariable, telemetryClient).ConfigureAwait(false);
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(path);
            return blob;
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

        public static async Task WriteToBlobUsingMsiAsync(string container, string path, string content, string storageAccountNameEnvironmentVariable, ITelemetryClient telemetryClient = null)
        {
            CloudBlockBlob outputBlob = await GetBlobUsingMsiAsync(container, path, storageAccountNameEnvironmentVariable, telemetryClient).ConfigureAwait(false);
            CloudBlobStream cloudBlobStream = await outputBlob.OpenWriteAsync().ConfigureAwait(false);
            using (StreamWriter writer = new StreamWriter(cloudBlobStream, Encoding.UTF8))
            {
                await writer.WriteLineAsync(content).ConfigureAwait(false);
            }
        }

        public static async Task<CloudBlobContainer> GetBlobContainerUsingMsiAsync(string container, string storageAccountNameEnvironmentVariable, ITelemetryClient telemetryClient = null)
        {
            CloudStorageAccount storageAccount = await GetStorageAccountUsingMsiAsync(storageAccountNameEnvironmentVariable, telemetryClient).ConfigureAwait(false);
            CloudBlobClient storageBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = storageBlobClient.GetContainerReference(container);
            return blobContainer;
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

        public static async Task<CloudBlobContainer> GetStorageContainerUsingMsiAsync(string container, string storageAccountNameEnvironmentVariable, ITelemetryClient telemetryClient = null)
        {
            CloudBlobContainer storageContainer = await GetBlobContainerUsingMsiAsync(container, storageAccountNameEnvironmentVariable, telemetryClient).ConfigureAwait(false);
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

        public static async Task<CloudQueue> GetStorageQueueCachedUsingMsiAsync(string queueName, string storageAccountNameEnvironmentVariable, ITelemetryClient telemetryClient = null)
        {
            await CloudResourceLock.WaitAsync().ConfigureAwait(false);
            try
            {
                string key = $"{queueName}:{storageAccountNameEnvironmentVariable}";
                if (!CloudQueues.TryGetValue(key, out CachedCloudQueue cachedCloudQueue) || (DateTime.UtcNow - cachedCloudQueue.LastInitializationDateUtc) >= CloudResourcesInitializitonFrequency)
                {
                    CloudQueue cloudQueue = await GetStorageQueueUsingMsiAsync(queueName, storageAccountNameEnvironmentVariable, telemetryClient).ConfigureAwait(false);
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

        public static async Task<CloudQueue> GetStorageQueueUsingMsiAsync(string queueName, string storageAccountNameEnvironmentVariable, ITelemetryClient telemetryClient)
        {
            CloudStorageAccount storageAccount = await GetStorageAccountUsingMsiAsync(storageAccountNameEnvironmentVariable, telemetryClient, isQueue: true).ConfigureAwait(false);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(queueName);
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
            return queue;
        }

        public static async Task<CloudQueue> GetStorageQueueAsync(string queueName, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            CloudStorageAccount storageAccount = GetStorageAccount(storageConnectionEnvironmentVariable);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(queueName);
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
            return queue;
        }

        public static async Task<List<CloudQueue>> ListStorageQueuesUsingMsiAsync(string prefix, string storageAccountNameEnvironmentVariable, ITelemetryClient telemetryClient = null)
        {
            List<CloudQueue> result = new List<CloudQueue>();

            CloudStorageAccount storageAccount = await GetStorageAccountUsingMsiAsync(storageAccountNameEnvironmentVariable, telemetryClient, isQueue: true);
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

        public static async Task<List<CloudQueue>> ListStorageQueuesUsingMsiAsync(string storageAccountNameEnvironmentVariable, ITelemetryClient telemetryClient = null)
        {
            List<CloudQueue> result = new List<CloudQueue>();

            CloudStorageAccount storageAccount = await GetStorageAccountUsingMsiAsync(storageAccountNameEnvironmentVariable, telemetryClient, isQueue: true).ConfigureAwait(false);
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
            CloudStorageAccount storageAccount = GetStorageAccount(storageConnectionEnvironmentVariable);
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
            string phrase = GetBlobResource(storageAccountName);
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

        public static CloudStorageAccount GetStorageAccount(string storageConnectionEnvironmentVariable)
        {
            string stagingBlobConnectionString = Environment.GetEnvironmentVariable(storageConnectionEnvironmentVariable);
            return CloudStorageAccount.Parse(stagingBlobConnectionString);
        }

        public static async Task<CloudStorageAccount> GetStorageAccountUsingMsiAsync(string storageAccountNameEnvironmentVariable, ITelemetryClient telemetryClient, bool isQueue = false)
        {
            string storageAccountName = Environment.GetEnvironmentVariable(storageAccountNameEnvironmentVariable);
            string resource = isQueue ? GetQueueResource(storageAccountName) : GetBlobResource(storageAccountName);
            StorageCredentials creds = await GetStorageCredentails(resource, telemetryClient).ConfigureAwait(false);
            CloudStorageAccount storageAccount = new CloudStorageAccount(creds, storageAccountName, EndPointSuffix, true);
            return storageAccount;
        }

        private static async Task<StorageCredentials> GetStorageCredentails(string resource, ITelemetryClient telemetryClient)
        {
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            string token = await azureServiceTokenProvider.GetAccessTokenAsync(resource).ConfigureAwait(false);
            var jwt = new JwtSecurityToken(token);
            TokenExpiration = jwt.ValidTo;
            Dictionary<string, string> properties = new Dictionary<string, string>()
            {
                { "Resource", resource },
                { "TokenValidTo", tokenExpiration.ToString()}
            };
            telemetryClient?.TrackEvent("MsiTokenGeneration", properties);
            TokenCredential tokenCredential = new TokenCredential(token);
            return new StorageCredentials(tokenCredential);
        }

        public static string GetBlobResource(string storageAccountName)
        {
            return $"https://{storageAccountName}.blob.core.windows.net/";
        }

        public static string GetQueueResource(string storageAccountName)
        {
            return $"https://{storageAccountName}.queue.core.windows.net/";
        }
    }
}
