// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.IO;
using Microsoft.CloudMine.Core.Collectors.Telemetry;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public class AzureTableCache<T> : ICache<T> where T : TableEntityWithContext, new()
    {
        private readonly ITelemetryClient telemetryClient;
        private readonly string name;
        private readonly string storageConnectionEnvironmentVariable;

        private TableClient table;
        private bool initialized;

        public AzureTableCache(ITelemetryClient telemetryClient, string name, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            this.telemetryClient = telemetryClient;
            initialized = false;
            this.name = name;
            this.storageConnectionEnvironmentVariable = storageConnectionEnvironmentVariable;
        }

        public AzureTableCache(ITelemetryClient telemetryClient, TableClient table)
        {
            this.telemetryClient = telemetryClient;
            this.table = table;
            initialized = true;
            name = table.Name;
        }

        public async Task InitializeAsync()
        {
            if (initialized)
            {
                telemetryClient.LogWarning($"AzureTable ({name}).InitializeAsync was called after azure table was initialized. Ignoring the call.");
                return;
            }

            table = await AzureHelpers.GetStorageTableAsync(name, storageConnectionEnvironmentVariable).ConfigureAwait(false);

            initialized = true;
        }

        public async Task CacheAsync(T tableEntity)
        {
            if (!initialized)
            {
                telemetryClient.LogWarning($"AzureTable ({name}).CacheAsync was called before azure table was initialized. Ignoring the call.");
                return;
            }

            try
            {
                var result = await table.UpsertEntityAsync(tableEntity).ConfigureAwait(false);
                    Dictionary<string, string> properties = new Dictionary<string, string>(tableEntity.GetContext())
                    {
                        { "ErrorReturnCode", result.Status.ToString() },
                        { "Operation", "CacheAsync" },
                    };
                    telemetryClient.TrackEvent("CachingError", properties);
            }
            catch (Exception exception)
            {
                Dictionary<string, string> properties = new Dictionary<string, string>(tableEntity.GetContext())
                {
                    { "ErrorReturnCode", exception.ToString() },
                    { "ErrorType", exception.GetType().ToString() },
                    { "Operation", "CacheAsync" },
                };
                telemetryClient.TrackEvent("CachingError", properties);
            }
        }

        public async Task<bool> CacheAtomicAsync(T currentTableEntity, T newTableEntity)
        {
            if (currentTableEntity == null)
            {
                try
                {
                    await table.AddEntityAsync(newTableEntity).ConfigureAwait(false);
                    return true;
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>
                    {
                        { "ErrorMessage", ex.Message },
                        { "ErrorReturnCode", ex.Status.ToString() },
                        { "Operation", "CacheAtomicAsync" },
                    };
                    telemetryClient.TrackEvent("CachingError", properties);
                }

                return false;
            }

            try
            {
                await table.UpdateEntityAsync(newTableEntity, currentTableEntity.ETag, TableUpdateMode.Replace).ConfigureAwait(false);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.PreconditionFailed)
            {
                    Dictionary<string, string> properties = new Dictionary<string, string>
                    {
                        { "ErrorMessage", ex.Message },
                        { "ErrorReturnCode", ex.Status.ToString() },
                        { "Operation", "CacheAtomicAsync" },
                    };
                    telemetryClient.TrackEvent("CachingError", properties);
            }
            return false;
        }

        public async Task<T> RetrieveAsync(T tableEntity)
        {
            if (!initialized)
            {
                telemetryClient.LogWarning($"AzureTable ({name}).CacheAsync was called before azure table was initialized. Ignoring the call.");
                return null;
            }

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
                    { "ErrorReturnCode", exception.ToString() }, { "ErrorType", exception.GetType().ToString() }, { "Operation", "RetrieveAsync" },
                };
                telemetryClient.TrackEvent("CachingError", properties);

                return null;
            }
        }

        public async Task<bool> ExistsAsync(T repositoryTableEntity)
        {
            T result = await RetrieveAsync(repositoryTableEntity).ConfigureAwait(false);
            return result != null;
        }
    }
}
