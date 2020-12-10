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

                string recordWriterType = recordWriterTypeToken.Value<string>();
                switch (recordWriterType)
                {
                    case "AzureDataLakeStorageV1":
                        JToken rootFolderToken = recordWriterToken.SelectToken("RootFolder");
                        JToken versionToken = recordWriterToken.SelectToken("Version");
                        if (rootFolderToken == null)
                        {
                            throw new FatalTerminalException("AzureDataLakeStorageV1 must provide a RootFolder");
                        }

                        string rootFolder = rootFolderToken.Value<string>();
                        string version = versionToken == null ? "v3" : versionToken.Value<string>(); // Temporarily permit versionToken to be null since otherwise it is an API-breaking change between Core and GH collectors.
                        IRecordWriter adlsRecordWriter = new AdlsBulkRecordWriter<T>(adlsClient, identifier, telemetryClient, functionContext, contextWriter, root: rootFolder, version);
                        this.recordWriters.Add(adlsRecordWriter);
                        break;
                    case "AzureBlob":
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
                        IRecordWriter blobRecordWriter = this.ConstructAzureBlobWriter(rootContainer, outputQueueName, identifier, telemetryClient, functionContext, contextWriter, storageConnectionEnvironmentVariable, notificationQueueEnvironmentVariable);
                        this.recordWriters.Add(blobRecordWriter);
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
