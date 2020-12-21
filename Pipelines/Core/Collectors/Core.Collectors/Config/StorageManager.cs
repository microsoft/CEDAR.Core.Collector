﻿// Copyright (c) Microsoft Corporation.
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


        private IRecordWriter InitializeAzureBlobWriter<T>(JToken recordWriterToken, string identifier, T functionContext, ContextWriter<T> contextWriter) where T : FunctionContext
        {
            JToken rootContainerToken = recordWriterToken.SelectToken("RootContainer");
            JToken outputQueueNameToken = recordWriterToken.SelectToken("OutputQueueName");
            if (rootContainerToken == null || outputQueueNameToken == null)
            {
                throw new FatalTerminalException("AzureBlob storage must provide a RootContainer and an OutputQueueName.");
            }

            JToken storageConnectionEnvironmentVariableToken = recordWriterToken.SelectToken("StorageConnectionEnvironmentVariable");
            JToken notificationQueueEnvironmentVariableToken = recordWriterToken.SelectToken("NotificationQueueEnvironmentVariable");

            // The following are optional (only used in Azure DevOps right now), so permit these values (tokens) to be null.
            string storageConnectionEnvironmentVariable = storageConnectionEnvironmentVariableToken?.Value<string>();
            string notificationQueueEnvironmentVariable = notificationQueueEnvironmentVariableToken?.Value<string>();

            string rootContainer = rootContainerToken.Value<string>();
            string outputQueueName = outputQueueNameToken.Value<string>();
            return this.ConstructAzureBlobWriter(rootContainer, outputQueueName, identifier, this.telemetryClient, functionContext, contextWriter, storageConnectionEnvironmentVariable, notificationQueueEnvironmentVariable);
        }

        private IRecordWriter InitializeAdlsBulkRecordWriter<T>(JToken recordWriterToken, AdlsClient adlsClient, string identifier, T functionContext, ContextWriter<T> contextWriter) where T : FunctionContext
        {
            JToken rootFolderToken = recordWriterToken.SelectToken("RootFolder");
            JToken versionToken = recordWriterToken.SelectToken("Version");
            if (rootFolderToken == null || versionToken == null)
            {
                throw new FatalTerminalException("AzureDataLakeStorageV1 must provide RootFolder and Version.");
            }

            string rootFolder = rootFolderToken.Value<string>();
            string version = versionToken.Value<string>();
            return new AdlsBulkRecordWriter<T>(adlsClient, identifier, this.telemetryClient, functionContext, contextWriter, root: rootFolder, version);
        }

        protected virtual AzureBlobRecordWriter<T> ConstructAzureBlobWriter<T>(string rootContainer,
                                                                               string outputQueueName,
                                                                               string identifier,
                                                                               ITelemetryClient telemetryClient,
                                                                               T functionContext,
                                                                               ContextWriter<T> contextWriter,
                                                                               string storageConnectionEnvironmentVariable,
                                                                               string notificationQueueEnvironmentVariable) where T : FunctionContext
        {
            return new AzureBlobRecordWriter<T>(rootContainer, outputQueueName, identifier, telemetryClient, functionContext, contextWriter);
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
