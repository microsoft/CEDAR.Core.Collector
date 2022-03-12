// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Cache;
using Microsoft.CloudMine.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Collector
{
    public abstract class CachingCollector<TCollectionNode, TEndpointProgressTableEntity> where TCollectionNode : CollectionNode
                                                                                          where TEndpointProgressTableEntity : ProgressTableEntity
    {
        private readonly CollectorBase<TCollectionNode> collector;
        private readonly ITelemetryClient telemetryClient;
        private readonly List<Exception> exceptions;

        public Exception Exception { get; private set; }

        public CachingCollector(CollectorBase<TCollectionNode> collector, ITelemetryClient telemetryClient)
        {
            this.collector = collector;
            this.telemetryClient = telemetryClient;
            this.exceptions = new List<Exception>();
        }

        public async Task<bool> ProcessAndCacheAsync(TCollectionNode collectionNode, TEndpointProgressTableEntity progressRecord, bool ignoreCache, bool scheduledCollection)
        {
            if (!ignoreCache && !scheduledCollection)
            {
                // If ignoreCache == true, then skip cache check and force (re-)collection.
                // If scheduledCollection == true, then skip cache check (no need since by design there should not be any cache entry for it) and force collection.

                TEndpointProgressTableEntity cachedProgressRecord = await this.RetrieveAsync(progressRecord).ConfigureAwait(false);
                if (cachedProgressRecord != null && cachedProgressRecord.Succeeded)
                {
                    // If the cache includes the progress record and record.Succeeded is true, skip re-collection since this endpoint was previously collected successfully.

                    Dictionary<string, string> properties = new Dictionary<string, string>()
                    {
                        { "ApiName", collectionNode.ApiName },
                        { "RecordType", collectionNode.RecordType },
                    };
                    this.telemetryClient.TrackEvent("SkippedCollection", properties);
                    return true;
                }
            }

            progressRecord.Succeeded = false;
            try
            {
                await this.collector.ProcessAsync(collectionNode).ConfigureAwait(false);
                progressRecord.Succeeded = true;
                await this.CacheAsync(progressRecord).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception)
            {
                this.Exception = exception;
                this.exceptions.Add(exception);
                this.telemetryClient.TrackException(exception, $"Failed to process and cache endpoint '{collectionNode.ApiName}'.");
                await this.CacheAsync(progressRecord).ConfigureAwait(false);
                return false;
            }
        }

        public void Remove_Exception(Exception exception)
        {
            this.exceptions.Remove(exception);
        }

        protected abstract Task<TEndpointProgressTableEntity> RetrieveAsync(TEndpointProgressTableEntity progressRecord);

        protected abstract Task CacheAsync(TEndpointProgressTableEntity progressRecord);

        public void FinalizeCollection()
        {
            if (this.exceptions.Count == 0)
            {
                return;
            }

            throw new AggregateException(this.exceptions);
        }
    }
}
