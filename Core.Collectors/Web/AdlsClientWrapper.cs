// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Storage;
using Azure.Storage.Files.DataLake;
using Microsoft.Rest.Azure.Authentication;
using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public interface IAdlsClient
    {
        public DataLakeServiceClient serviceClient { get; }
    }

    public class AdlsClientWrapper : IAdlsClient
    {
        public static readonly Uri serviceUri = new Uri(@"https://datalake.azure.net/");

        public DataLakeServiceClient serviceClient { get; private set; }

        // This is the old AdlsClientWrapper API.
        // The old API will be deprecated soon, but to protect our jobs from failing, we need to support both the old API and new API.
        public AdlsClientWrapper()
            : this(settings: null)
        {
        }

        public AdlsClientWrapper(string settings)
        {
            if (settings == null)
            {
                // Some products that use ADLS Client does not have config-based settings, e.g., RecordOutputService. For these products, also support having a 'null' setting and loading config from environment variables (as before).
                this.LoadSettingsFromEnvironmentVariables();
                return;
            }

            JObject config = JObject.Parse(settings);
            JToken adlsClientToken = config.SelectToken("AzureDataLakeStorage");
            if (adlsClientToken == null)
            {
                // Don't fail loading the function app environment if these variables are not provided. Instead don't initialize the ADLS client.
                // This way, we permit functions that don't depend on the ADLS client to still run without configuring the ADLS client.
                // Once the function loads, we will also log the fact that ADLS client is not initialized separately.
                return;
            }

            JToken storageAccountNameToken = adlsClientToken.SelectToken("AccountName");
            JToken storageAccountKeyToken = adlsClientToken.SelectToken("AccountKey");

            string storageAccountName = storageAccountNameToken.Value<string>();
            string storageAccountKey = storageAccountKeyToken.Value<string>();

            this.serviceClient = InitializeAdlsClient(storageAccountName, storageAccountKey);
        }

        private void LoadSettingsFromEnvironmentVariables()
        {
            string storageAccountName = Environment.GetEnvironmentVariable("StorageAccountName");

            string storageAccountKey = Environment.GetEnvironmentVariable("StorageAccountKey");

            StorageSharedKeyCredential sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, storageAccountKey);

            this.serviceClient = new DataLakeServiceClient(serviceUri, sharedKeyCredential);
        }

        internal static DataLakeServiceClient InitializeAdlsClient(string storageAccountName, string storageAccountKey)
        {
            ActiveDirectoryServiceSettings serviceSettings = ActiveDirectoryServiceSettings.Azure;
            serviceSettings.TokenAudience = serviceUri;

            StorageSharedKeyCredential sharedKeyCredential;
            try
            {
                sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, storageAccountKey);
            }
            catch (Exception)
            {
                throw new AggregateException($"Cannot get credentials for ADLS client.");
            }

            return new DataLakeServiceClient(serviceUri, sharedKeyCredential);
        }
    }
}
