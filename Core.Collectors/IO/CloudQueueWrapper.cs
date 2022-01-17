// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Storage.Queues;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public class CloudQueueWrapper : IQueue
    {
        private readonly QueueClient queue;

        public CloudQueueWrapper(QueueClient queue)
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
            return this.queue.SendMessageAsync(message);
        }

        public Task PutMessageAsync(string message, TimeSpan timeToLive)
        {
            return this.queue.SendMessageAsync(message, null, timeToLive);
        }

        public Task PutObjectAsJsonStringAsync(object obj, TimeSpan timeToLive)
        {
            string message = JsonConvert.SerializeObject(obj);
            return this.PutMessageAsync(message, timeToLive);
        }
    }
}
