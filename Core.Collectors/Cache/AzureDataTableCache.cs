﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.CloudMine.Core.Collectors.IO;
using Microsoft.CloudMine.Core.Collectors.Telemetry;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public class AzureDataTableCache<T> : ICache<T> where T : DataTableEntityWithContext, new()
    {
        private readonly ITelemetryClient telemetryClient;
        private readonly string name;
        private readonly string storageConnectionEnvironmentVariable;

        private TableClient table;
        private bool initialized;

        public AzureDataTableCache(ITelemetryClient telemetryClient, string name, string storageConnectionEnvironmentVariable = "AzureWebJobsStorage")
        {
            this.telemetryClient = telemetryClient;
            initialized = false;
            this.name = name;
            this.storageConnectionEnvironmentVariable = storageConnectionEnvironmentVariable;
        }

        public AzureDataTableCache(ITelemetryClient telemetryClient, TableClient table)
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

            table = await AzureHelpers.GetTableClientAsync(name, storageConnectionEnvironmentVariable).ConfigureAwait(false);

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
                    { "ErrorReturnCode", result.Status.ToString() }, { "Operation", "CacheAsync" },
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
                var result = await table.GetEntityAsync<T>(tableEntity.PartitionKey, tableEntity.RowKey).ConfigureAwait(false);
                return result.Value;
            }
            catch (Exception exception)
            {
                Dictionary<string, string> properties = new Dictionary<string, string>(tableEntity.GetContext())
                {
                    { "ErrorReturnCode", exception.ToString() },
                    { "ErrorType", exception.GetType().ToString() },
                    { "Operation", "RetrieveAsync" },
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