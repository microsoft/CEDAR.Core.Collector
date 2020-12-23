// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Cache;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Cache
{
    public class InMemoryCache<T> : ICache<T> where T : TableEntityWithContext
    {
        private readonly Dictionary<string, Dictionary<string, T>> cache;

        public int LookupCount { get; private set; }
        public int InsertCount { get; private set; }

        public InMemoryCache()
        {
            this.cache = new Dictionary<string, Dictionary<string, T>>();
            this.InsertCount = 0;
            this.LookupCount = 0;
        }

        public Task CacheAsync(T tableEntity)
        {
            this.InsertCount++;

            string partitionKey = tableEntity.PartitionKey;
            if (!this.cache.TryGetValue(partitionKey, out Dictionary<string, T> innerCache))
            {
                innerCache = new Dictionary<string, T>();
                this.cache.Add(partitionKey, innerCache);
            }

            innerCache[tableEntity.RowKey] = tableEntity;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(T tableEntity)
        {
            return Task.FromResult(this.Retrieve(tableEntity) != null);
        }

        public Task InitializeAsync()
        {
            // Assume successful.
            return Task.CompletedTask;
        }

        public Task<T> RetrieveAsync(T tableEntity)
        {
            return Task.FromResult(this.Retrieve(tableEntity));
        }

        public T Retrieve(T tableEntity)
        {
            this.LookupCount++;

            if (this.cache.TryGetValue(tableEntity.PartitionKey, out Dictionary<string, T> innerCache))
            {
                if (innerCache.TryGetValue(tableEntity.RowKey, out T result))
                {
                    return result;
                }

                return null;
            }

            return null;
        }

        public async Task<bool> CacheAtomicAsync(T currentTableEntity, T newTableEntity)
        {
            // Assume the same as CacheAsync
            await this.CacheAsync(newTableEntity).ConfigureAwait(false);
            return true;
        }
    }
}
