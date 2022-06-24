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
        private readonly CloudQueue queue;
        private readonly string queueName;
        private readonly string storageAccountNameEnvironmentVariable;

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
            CloudQueue queue = await AzureHelpers.GetStorageQueueUsingMsiAsync(queueName, storageAccountNameEnvironmentVariable).ConfigureAwait(false);
            await queue.AddMessageAsync(new CloudQueueMessage(message)).ConfigureAwait(false);
        }

        public async Task PutMessageAsync(string message, TimeSpan timeToLive)
        {
            CloudQueue queue = await AzureHelpers.GetStorageQueueUsingMsiAsync(queueName, storageAccountNameEnvironmentVariable).ConfigureAwait(false);
            await queue.AddMessageAsync(new CloudQueueMessage(message), timeToLive, null, new QueueRequestOptions(), new OperationContext());
        }

        public Task PutObjectAsJsonStringAsync(object obj, TimeSpan timeToLive)
        {
            string message = JsonConvert.SerializeObject(obj);
            return this.PutMessageAsync(message, timeToLive);
        }
    }
}
