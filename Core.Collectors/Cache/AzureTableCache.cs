// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.CloudMine.Core.Collectors.IO;
using Microsoft.CloudMine.Core.Collectors.Telemetry;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public class AzureTableCache<T> : ICache<T> where T : TableEntityWithContext
    {
        private readonly ITelemetryClient telemetryClient;
        private readonly string name;
        private readonly string storageConnectionEnvironmentVariable;

        private CloudTable table;
        private bool initialized;

        public AzureTableCache(ITelemetryClient telemetryClient, string name, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            this.telemetryClient = telemetryClient;
            this.initialized = false;
            this.name = name;
            this.storageConnectionEnvironmentVariable = storageConnectionEnvironmentVariable;
        }

        public async Task InitializeAsync()
        {
            if (this.initialized)
            {
                this.telemetryClient.LogWarning($"AzureTable ({this.name}).InitializeAsync was called after azure table was initialized. Ignoring the call.");
                return;
            }

            this.table = await AzureHelpers.GetStorageTableAsync(name, this.storageConnectionEnvironmentVariable).ConfigureAwait(false);

            this.initialized = true;
        }

        public async Task CacheAsync(T tableEntity)
        {
            if (!this.initialized)
            {
                this.telemetryClient.LogWarning($"AzureTable ({this.name}).CacheAsync was called before azure table was initialized. Ignoring the call.");
                return;
            }

            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(tableEntity);
            try
            {
                TableResult insertOrReplaceResult = await this.table.ExecuteAsync(insertOrReplaceOperation).ConfigureAwait(false);
                int insertOrReplaceStatusCode = insertOrReplaceResult.HttpStatusCode;
                // 204: no content => InsertOrReplace operation does not return any content when successful.
                if (insertOrReplaceStatusCode != 204)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>(tableEntity.GetContext())
                    {
                        { "ErrorReturnCode", insertOrReplaceStatusCode.ToString() },
                        { "Operation", "CacheAsync" },
                    };
                    this.telemetryClient.TrackEvent("CachingError", properties);
                }
            }
            catch (Exception exception)
            {
                Dictionary<string, string> properties = new Dictionary<string, string>(tableEntity.GetContext())
                {
                    { "ErrorReturnCode", exception.ToString() },
                    { "ErrorType", exception.GetType().ToString() },
                    { "Operation", "CacheAsync" },
                };
                this.telemetryClient.TrackEvent("CachingError", properties);
            }
        }

        public async Task<T> RetrieveAsync(T tableEntity)
        {
            if (!this.initialized)
            {
                this.telemetryClient.LogWarning($"AzureTable ({this.name}).CacheAsync was called before azure table was initialized. Ignoring the call.");
                return null;
            }

            TableOperation retrieveOperation = TableOperation.Retrieve<T>(tableEntity.PartitionKey, tableEntity.RowKey);
            try
            {
                TableResult retrieveResult = await this.table.ExecuteAsync(retrieveOperation).ConfigureAwait(false);
                int retrieveStatusCode = retrieveResult.HttpStatusCode;
                // 200: OK => The item exists in the cache and retrieve was successful.
                // 404: Does not exist => The item does not exist in the cache.
                if (retrieveStatusCode != 200 && retrieveStatusCode != 404) 
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>(tableEntity.GetContext())
                    {
                        { "ErrorReturnCode", retrieveStatusCode.ToString() },
                        { "Operation", "RetrieveAsync" },
                    };
                    this.telemetryClient.TrackEvent("CachingError", properties);
                }

                return (T)retrieveResult.Result;
            }
            catch (Exception exception)
            {
                Dictionary<string, string> properties = new Dictionary<string, string>(tableEntity.GetContext())
                {
                    { "ErrorReturnCode", exception.ToString() },
                    { "ErrorType", exception.GetType().ToString() },
                    { "Operation", "RetrieveAsync" },
                };
                this.telemetryClient.TrackEvent("CachingError", properties);

                return null;
            }
        }

        public async Task<bool> ExistsAsync(T repositoryTableEntity)
        {
            T result = await this.RetrieveAsync(repositoryTableEntity).ConfigureAwait(false);
            return result != null;
        }
    }
}
