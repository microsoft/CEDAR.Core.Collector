// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public class CloudQueueWrapper : IQueue
    {
        private readonly CloudQueue queue;

        public CloudQueueWrapper(CloudQueue queue)
        {
            this.queue = queue;
        }

        public Task PutObjectAsJsonStringAsync(object obj)
        {
            string message = JsonConvert.SerializeObject(obj);
            return this.PutMessageAsync(message);
        }

        public Task PutMessageAsync(string message)
        {
            return this.queue.AddMessageAsync(new CloudQueueMessage(message));
        }

        public Task PutMessageAsync(string message, TimeSpan timeToLive)
        {
            return this.queue.AddMessageAsync(new CloudQueueMessage(message), timeToLive, null, new QueueRequestOptions(), new OperationContext());
        }

        public Task PutObjectAsJsonStringAsync(object obj, TimeSpan timeToLive)
        {
            string message = JsonConvert.SerializeObject(obj);
            return this.PutMessageAsync(message, timeToLive);
        }
    }
}
