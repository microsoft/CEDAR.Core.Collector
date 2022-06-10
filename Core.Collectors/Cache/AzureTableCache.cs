// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.IO;
using Microsoft.CloudMine.Core.Telemetry;
using Microsoft.WindowsAzure.Storage;
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
        private readonly string storageAccountEnvironmentVariable;

        private CloudTable table;
        private bool initialized;

        public AzureTableCache(ITelemetryClient telemetryClient, string name, string storageAccountEnvironmentVariable = "StorageAccountName")
        {
            this.telemetryClient = telemetryClient;
            this.initialized = false;
            this.name = name;
            this.storageAccountEnvironmentVariable = storageAccountEnvironmentVariable;
        }

        public AzureTableCache(ITelemetryClient telemetryClient, CloudTable table)
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

            this.table = await AzureHelpers.GetStorageTableUsingMsiAsync(this.name, this.storageAccountEnvironmentVariable).ConfigureAwait(false);

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

        public async Task<bool> CacheAtomicAsync(T currentTableEntity, T newTableEntity)
        {
            if (currentTableEntity == null)
            {
                TableOperation insertOperation = TableOperation.Insert(newTableEntity);
                try
                {
                    await this.table.ExecuteAsync(insertOperation).ConfigureAwait(false);
                    return true;
                }
                catch (Exception insertException) when (insertException is StorageException)
                {
                    StorageException insertStorageException = (StorageException)insertException;
                    int insertStatusCode = insertStorageException.RequestInformation.HttpStatusCode;
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

            string currentETag = currentTableEntity.ETag;
            newTableEntity.ETag = currentETag;
            TableOperation replaceOperation = TableOperation.Replace(newTableEntity);
            try
            {
                await this.table.ExecuteAsync(replaceOperation).ConfigureAwait(false);
                return true;
            }
            catch (Exception replaceException) when (replaceException is StorageException)
            {
                StorageException replaceStorageException = (StorageException)replaceException;
                int replaceStatusCode = replaceStorageException.RequestInformation.HttpStatusCode;
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
