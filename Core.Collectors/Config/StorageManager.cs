// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.DataLake.Store;
using Microsoft.CloudMine.Core.Collectors.Context;
using Microsoft.CloudMine.Core.Collectors.Error;
using Microsoft.CloudMine.Core.Collectors.IO;
using Microsoft.CloudMine.Core.Collectors.Telemetry;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Config
{
    public class StorageManager : IDisposable
    {
        private readonly JArray recordWritersArray;
        private List<IRecordWriter> recordWriters;
        private readonly ITelemetryClient telemetryClient;
        private bool initialized;
        
        public StorageManager(JArray recordWritersArray, ITelemetryClient telemetryClient)
        {
            this.recordWritersArray = recordWritersArray;
            this.recordWriters = new List<IRecordWriter>();
            this.telemetryClient = telemetryClient;
            this.initialized = false;
        }

        public List<IRecordWriter> InitializeRecordWriters<T>(string identifier, T functionContext, ContextWriter<T> contextWriter, AdlsClient adlsClient) where T : FunctionContext
        {
            if (this.initialized)
            {
                throw new FatalTerminalException("StorageManager.InitializeRecordWriters has already been called, this operation should only be called once.");
            }
            this.initialized = true;
            this.recordWriters = new List<IRecordWriter>();
            foreach (JToken recordWriterToken in recordWritersArray)
            {
                JToken recordWriterTypeToken = recordWriterToken.SelectToken("Type");
                if (recordWriterTypeToken == null)
                {
                    throw new FatalTerminalException("Settings.json must provide a Type for storage locations.");
                }
                RecordWriterType recordWriterType = Enum.Parse<RecordWriterType>(recordWriterTypeToken.Value<string>());
                switch (recordWriterType)
                {
                    case RecordWriterType.AzureDataLakeStorageV1:
                        IRecordWriter adlsRecordWriter = this.InitializeAdlsBulkRecordWriter(recordWriterToken, adlsClient, identifier, functionContext, contextWriter);
                        this.recordWriters.Add(adlsRecordWriter);
                        break;
                    case RecordWriterType.AzureBlob:
                        IRecordWriter blobRecordWriter = this.InitializeAzureBlobWriter(recordWriterToken, identifier, functionContext, contextWriter);
                        this.recordWriters.Add(blobRecordWriter);
                        break;
                    case RecordWriterType.SplitAzureBlob:
                        IRecordWriter splitBlobRecordWriter = this.InitializeSplitAzureBlobWriter(recordWriterToken, functionContext, contextWriter);
                        this.recordWriters.Add(splitBlobRecordWriter);
                        break;
                    default:
                        throw new FatalTerminalException($"Unsupported Storage Type : {recordWriterType}");
                }
            }
            if (this.recordWriters.Count == 0)
            {
                throw new FatalTerminalException("No valid record writers are provided in Settings.json.");
            }
            return new List<IRecordWriter>(this.recordWriters);
        }

        public List<IRecordWriter> InitializeRecordWriters<T>(T functionContext, ContextWriter<T> contextWriter) where T : FunctionContext
        {
            if (this.initialized)
            {
                throw new FatalTerminalException("StorageManager.InitializeRecordWriters has already been called, this operation should only be called once.");
            }

            this.initialized = true;
            this.recordWriters = new List<IRecordWriter>();
            foreach (JToken recordWriterToken in recordWritersArray)
            {
                JToken recordWriterTypeToken = recordWriterToken.SelectToken("Type");
                if (recordWriterTypeToken == null)
                {
                    throw new FatalTerminalException("Settings.json must provide a Type for storage locations.");
                }

                RecordWriterType recordWriterType = Enum.Parse<RecordWriterType>(recordWriterTypeToken.Value<string>());
                switch (recordWriterType)
                {
                    case RecordWriterType.SplitAzureBlob:
                        IRecordWriter splitBlobRecordWriter = this.InitializeSplitAzureBlobWriter(recordWriterToken, functionContext, contextWriter);
                        this.recordWriters.Add(splitBlobRecordWriter);
                        break;
                    default:
                        throw new FatalTerminalException($"Unsupported Storage Type : {recordWriterType}");
                }
            }

            if (this.recordWriters.Count == 0)
            {
                throw new FatalTerminalException("No valid record writers are provided in Settings.json.");
            }

            return new List<IRecordWriter>(this.recordWriters);
        }
        private IRecordWriter InitializeSplitAzureBlobWriter<T>(JToken recordWriterToken, T functionContext, ContextWriter<T> contextWriter) where T : FunctionContext
        {
            JToken rootContainerToken = recordWriterToken.SelectToken("RootContainer");
            JToken notificationQueuePrefixToken = recordWriterToken.SelectToken("NotificationQueuePrefix");
            JToken storageConnectionEnvironmentVariableToken = recordWriterToken.SelectToken("StorageConnectionEnvironmentVariable");
            if (rootContainerToken == null || notificationQueuePrefixToken == null || storageConnectionEnvironmentVariableToken == null)
            {
                throw new FatalTerminalException("SplitAzureBlob storage must provide a RootContainer, a NotificationQueuePrefix, and a StorageConnectionEnvironmentVariable.");
            }

            string rootContainer = rootContainerToken.Value<string>();
            string notificationQueuePrefix = notificationQueuePrefixToken.Value<string>();
            string storageConnectionEnvironmentVariable = storageConnectionEnvironmentVariableToken.Value<string>();
            return new SplitAzureBlobRecordWriter<T>(rootContainer, notificationQueuePrefix, this.telemetryClient, functionContext, contextWriter, storageConnectionEnvironmentVariable);
        }

        /// <summary>
        /// Important Note: Ensure that the StorageManager is properly disposed (which disposes the underlying record writers before this method is called.
        ///                 Once the record writers are finalized, further operations (e.g., Dispose) will be ignored.
        /// </summary>
        public async Task FinalizeRecordWritersAsync()
        {
            foreach (IRecordWriter recordWriter in this.recordWriters)
            {
                await recordWriter.FinalizeAsync().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            foreach (IRecordWriter recordWriter in this.recordWriters)
            {
                try
                {
                    recordWriter.Dispose();
                }
                catch (Exception exception)
                {
                    this.telemetryClient.TrackException(exception, $"Failed to dispose of RecordWriter : {recordWriter.GetType()}");
                }   
            }
        }
    }
}
