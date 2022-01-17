// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.WebJobs.Host.Queues;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using System.Threading;
using System.Globalization;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs.Host;

namespace Microsoft.CloudMine.Core.Collectors.Collector
{
    /// <summary>
    /// The default queue processor in Azure Functions move the poison messages to the poison queue with the default time-to-live, which is 7 days and this is not configurable.
    /// We want to have infinite time to live for queue messages and therefore implement our own custom queue processor (and queue processor factory).
    /// </summary>
    public class CustomQueueProcessorFactory : IQueueProcessorFactory
    {
        public QueueProcessor Create(QueueProcessorOptions queueProcessorOptions)
        {
            return new CustomQueueProcessor(queueProcessorOptions);
        }
    }

    public class CustomQueueProcessor : QueueProcessor
    {
        private readonly ILogger _logger;
        private QueuesOptions QueuesOptions { get;  set; }

        public CustomQueueProcessor(QueueProcessorOptions queueProcessorOptions)
            : base(queueProcessorOptions)
        {
            _logger = queueProcessorOptions.Logger;
            QueuesOptions = queueProcessorOptions.Options;
        }

        /// <summary>
        /// Base implementation is taken from the DefaultQueueProcessor:
        /// https://github.com/Azure/azure-webjobs-sdk/blob/50df9323e730c62207b85273712081cf9803f8c2/src/Microsoft.Azure.WebJobs.Extensions.Storage/Queues/QueueProcessor.cs#L161
        /// </summary>
        protected override async Task CopyMessageToPoisonQueueAsync(QueueMessage message, QueueClient poisonQueue, CancellationToken cancellationToken)
        {
            string msg = string.Format(CultureInfo.InvariantCulture, "Message has reached MaxDequeueCount of {0}. Moving message to queue '{1}'.", QueuesOptions.MaxDequeueCount, poisonQueue.Name);
            _logger?.LogWarning(msg);

            try
            {
                await poisonQueue.CreateIfNotExistsAsync().ConfigureAwait(false);
            }
            catch
            {
                // Do this as a best-effort. This can fail e.g., due to multiple functions trying to do this at the same time.
            }
            await poisonQueue.SendMessageAsync(message.Body, null, timeToLive: TimeSpan.MaxValue).ConfigureAwait(false);

            var eventArgs = new PoisonMessageEventArgs(message, poisonQueue);
            await OnMessageAddedToPoisonQueueAsync(eventArgs).ConfigureAwait(false);
        }
    }
}
