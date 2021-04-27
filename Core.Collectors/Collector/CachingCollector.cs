// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Cache;
using Microsoft.CloudMine.Core.Collectors.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Collector
{
    public abstract class CachingCollector<TCollectionNode, TEndpointProgressTableEntity> where TCollectionNode : CollectionNode
                                                                                          where TEndpointProgressTableEntity : ProgressTableEntity
    {
        private readonly CollectorBase<TCollectionNode> collector;
        private readonly ICache<TEndpointProgressTableEntity> progressCache;
        private readonly ITelemetryClient telemetryClient;
        private readonly List<Exception> exceptions;

        public Exception Exception { get; private set; }

        public CachingCollector(CollectorBase<TCollectionNode> collector, ICache<TEndpointProgressTableEntity> progressCache, ITelemetryClient telemetryClient)
        {
            this.collector = collector;
            this.progressCache = progressCache;
            this.telemetryClient = telemetryClient;
            this.exceptions = new List<Exception>();
        }

        public async Task<bool> ProcessAndCacheAsync(TCollectionNode collectionNode, TEndpointProgressTableEntity progressRecord, bool ignoreCache, bool scheduledCollection)
        {
            if (!ignoreCache && !scheduledCollection)
            {
                // If ignoreCache == true, then skip cache check and force (re-)collection.
                // If scheduledCollection == true, then skip cache check (no need since by design there should not be any cache entry for it) and force collection.

                TEndpointProgressTableEntity cachedProgressRecord = await this.progressCache.RetrieveAsync(progressRecord).ConfigureAwait(false);
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
                await this.progressCache.CacheAsync(progressRecord).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception)
            {
                this.Exception = exception;
                this.exceptions.Add(exception);
                this.telemetryClient.TrackException(exception, $"Failed to process and cache endpoint '{collectionNode.ApiName}'.");
                await this.progressCache.CacheAsync(progressRecord).ConfigureAwait(false);
                return false;
            }
        }

        public void Finalize()
        {
            if (this.exceptions.Count == 0)
            {
                return;
            }

            throw new AggregateException(this.exceptions);
        }
    }
}
