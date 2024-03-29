// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Telemetry;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public class CloudQueueMsiWrapper : IQueue
    {
        private readonly string queueName;
        private readonly string storageAccountNameEnvironmentVariable;
        private readonly ITelemetryClient telemetryClient;
        private CloudQueue queue;
        private DateTime msiTokenExpiration = DateTime.MinValue;

        public CloudQueueMsiWrapper(string queueName, string storageAccountNameEnvironmentVariable, ITelemetryClient telemetryClient)
        {
            this.queueName = queueName;
            this.storageAccountNameEnvironmentVariable = storageAccountNameEnvironmentVariable;
            this.telemetryClient = telemetryClient;
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

        public async Task<CloudQueue> GetValidMsiStorageQueueAsync()
        {
            // Tokens acquired via the App Authentication library currently are refreshed when less than 5 minutes remains until they expire. So it caches the token for 23 hours 55 minutes in memory.
            if (this.msiTokenExpiration - DateTime.UtcNow <= TimeSpan.FromMinutes(5))
            {
                CloudQueue cloudQueue = await AzureHelpers.GetStorageQueueUsingMsiAsync(this.queueName, this.storageAccountNameEnvironmentVariable, this.telemetryClient).ConfigureAwait(false);
                this.msiTokenExpiration = AzureHelpers.TokenExpiration;
                Dictionary<string, string> properties = new Dictionary<string, string>()
                {
                    { "MsiTokenExpirationTime", this.msiTokenExpiration.ToString() },
                    { "Queue", queueName },
                };
                telemetryClient.TrackEvent("RefreshedMsiToken", properties);
                this.queue = cloudQueue;
            }
            return this.queue;
        }
    }
}
