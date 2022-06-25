// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public class CloudQueueMsiWrapper : IQueue
    {
        private CloudQueue queue;
        private readonly string queueName;
        private readonly string storageAccountNameEnvironmentVariable;
        private DateTime lastMsiTokenRefreshDateUtc = DateTime.MinValue;
        private readonly static TimeSpan MsiTokenRefreshFrequency = TimeSpan.FromHours(23.9167);

        public CloudQueueMsiWrapper(CloudQueue queue, string storageAccountNameEnvironmentVariable)
        {
            this.queue = queue;
            this.queueName = queue.Name;
            this.storageAccountNameEnvironmentVariable = storageAccountNameEnvironmentVariable;
        }

        public Task PutObjectAsJsonStringAsync(object obj)
        {
            string message = JsonConvert.SerializeObject(obj);
            return this.PutMessageAsync(message);
        }

        public async Task PutMessageAsync(string message)
        {
            CloudQueue queue = await GetValidMsiStorageQueueAsync().ConfigureAwait(false);
            await queue.AddMessageAsync(new CloudQueueMessage(message)).ConfigureAwait(false);
        }

        public async Task PutMessageAsync(string message, TimeSpan timeToLive)
        {
            CloudQueue queue = await GetValidMsiStorageQueueAsync().ConfigureAwait(false);
            await queue.AddMessageAsync(new CloudQueueMessage(message), timeToLive, null, new QueueRequestOptions(), new OperationContext());
        }

        public Task PutObjectAsJsonStringAsync(object obj, TimeSpan timeToLive)
        {
            string message = JsonConvert.SerializeObject(obj);
            return this.PutMessageAsync(message, timeToLive);
        }

        private async Task<CloudQueue> GetValidMsiStorageQueueAsync()

        {
            TimeSpan elapsed = DateTime.UtcNow - this.lastMsiTokenRefreshDateUtc;
            //If the elapsed time >= 23 hours 55 mins, then fetch the queue with refreshed token. Tokens acquired via the App Authentication library currently are refreshed when less than 5 minutes remains until they expire. So it caches the token for 23 hours 55 minutes in memory.
            if (elapsed >= MsiTokenRefreshFrequency)
            {
                this.queue = await AzureHelpers.GetStorageQueueUsingMsiAsync(this.queueName, storageAccountNameEnvironmentVariable).ConfigureAwait(false);
                this.lastMsiTokenRefreshDateUtc = DateTime.UtcNow;
            }
            return this.queue;
        }
    }
}
