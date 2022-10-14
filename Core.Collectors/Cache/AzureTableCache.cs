// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Data.Tables;
using Microsoft.CloudMine.Core.Collectors.IO;
using Microsoft.CloudMine.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public class AzureTableCache<T> : ICache<T> where T : class, ITableEntityWithContext, new()
    {
        private readonly ITelemetryClient telemetryClient;
        private readonly string name;
        private readonly string storageConnectionEnvironmentVariable;

        private TableClient table;
        private bool initialized;

        public AzureTableCache(ITelemetryClient telemetryClient, string name, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            this.telemetryClient = telemetryClient;
            this.initialized = false;
            this.name = name;
            this.storageConnectionEnvironmentVariable = storageConnectionEnvironmentVariable;
        }

        public AzureTableCache(ITelemetryClient telemetryClient, TableClient table)
        {
            this.telemetryClient = telemetryClient;
            this.table = table;
            this.initialized = true;
            this.name = table.Name;
        }

        public async Task InitializeAsync()
        {
            if (this.initialized)
            {
                this.telemetryClient.LogWarning($"AzureTable ({this.name}).InitializeAsync was called after azure table was initialized. Ignoring the call.");
                return;
            }

            this.table = await AzureHelpers.GetStorageTableUsingMsiAsync(this.name, this.storageConnectionEnvironmentVariable).ConfigureAwait(false);

            this.initialized = true;
        }

        public async Task CacheAsync(T tableEntity)
        {
            if (!this.initialized)
            {
                this.telemetryClient.LogWarning($"AzureTable ({this.name}).CacheAsync was called before azure table was initialized. Ignoring the call.");
                return;
            }

            try
            {
                // TODO: validate TableUpdateMode
                Response insertOrReplaceResult = await this.table.UpsertEntityAsync(tableEntity, TableUpdateMode.Replace).ConfigureAwait(false);
                int insertOrReplaceStatusCode = insertOrReplaceResult.Status;
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

        public async Task<bool> CacheAtomicAsync(T currentTableEntity, T newTableEntity)
        {
            if (currentTableEntity == null)
            {
                try
                {
                    await this.table.UpsertEntityAsync(newTableEntity).ConfigureAwait(false);
                    return true;
                }
                catch (Exception insertException) when (insertException is RequestFailedException insertStorageException)
                {
                    int insertStatusCode = insertStorageException.Status;
                    if (insertStatusCode != 409) // Ignore 409, since it (Conflict) indicates that someone else did the update before us.
                    {
                        Dictionary<string, string> properties = new Dictionary<string, string>()
                        {
                            { "ErrorMessage", insertStorageException.Message },
                            { "ErrorReturnCode", insertStatusCode.ToString() },
                            { "Operation", "CacheAtomicAsync" },
                        };
                        this.telemetryClient.TrackEvent("CachingError", properties);
                    }
                }

                return false;
            }

            newTableEntity.ETag = currentTableEntity.ETag;
            try
            {
                await this.table.UpsertEntityAsync(newTableEntity, TableUpdateMode.Replace).ConfigureAwait(false);
                return true;
            }
            catch (Exception replaceException) when (replaceException is RequestFailedException replaceStorageException)
            {
                int replaceStatusCode = replaceStorageException.Status;
                if (replaceStatusCode != 412) // Ignore 412, since it (Pre-condition failed) indicates that someone else did the update before us.
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>()
                    {
                        { "ErrorMessage", replaceException.Message },
                        { "ErrorReturnCode", replaceStatusCode.ToString() },
                        { "Operation", "CacheAtomicAsync" },
                    };
                    this.telemetryClient.TrackEvent("CachingError", properties);
                }

                return false;
            }
        }

        public async Task<T> RetrieveAsync(T tableEntity)
        {
            if (!this.initialized)
            {
                this.telemetryClient.LogWarning($"AzureTable ({this.name}).CacheAsync was called before azure table was initialized. Ignoring the call.");
                return null;
            }

            try
            {
                Response<T> retrieveResult = await this.table.GetEntityAsync<T>(tableEntity.PartitionKey, tableEntity.RowKey).ConfigureAwait(false);
                int retrieveStatusCode = retrieveResult.GetRawResponse().Status;
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

                return retrieveResult.Value;
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
