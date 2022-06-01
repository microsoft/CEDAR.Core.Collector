// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Cache;
using Microsoft.CloudMine.Core.Collectors.Utility;
using Microsoft.CloudMine.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Collector
{
    public static class CachingCollectorUtils
    {
        public const int CacheLookupMultiplier = 2;

        internal static bool ShallIgnoreCache(bool ignoreCache, bool scheduledCollection, DateTime utcNow, DateTime sliceEndDateUtc, TimeSpan sliceCollectionFrequency)
        {
            // If ignoreCache == true, then skip cache check and force (re-)collection.

            // If scheduledCollection == true, then skip cache check if the collection is "recent" (no need since by design there should not be any cache entry for it) and force collection.
            // A collection is defined as a "recent" schedule if it is "scheduled" and if the end date of the schedule is more recent than the CacheLookupMultiplier * CollectionFrequency.
            // This is an optimization. Normally, "scheduled" collections are expected to be a cache miss, so we don't want to do a cache lookup for them (especially considering there will be
            // millions of them every hour). However, there had been cases previously where messages were put incorrectly that triggered a mass over-collection hours later. In this case,
            // if the collection seems to be "lagging", we would like to start looking up in the cache, just in case these had been already collected.
            bool recentSchedule = (utcNow - sliceEndDateUtc) <= (CacheLookupMultiplier * sliceCollectionFrequency);
            return ignoreCache || (scheduledCollection && recentSchedule);
        }
    }

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
            if (!CachingCollectorUtils.ShallIgnoreCache(ignoreCache, scheduledCollection, DateTime.UtcNow, progressRecord.EndDateUtc, progressRecord.CollectionFrequency))
            {
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
