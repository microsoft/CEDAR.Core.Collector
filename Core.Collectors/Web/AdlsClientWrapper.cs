// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Storage;
using Azure.Storage.Files.DataLake;
using Microsoft.CloudMine.Core.Collectors.Error;
using Microsoft.CloudMine.Core.Collectors.Utility;
using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public interface IAdlsClient
    {
        public DataLakeServiceClient AdlsClient { get; }
    }

    public class AdlsClientWrapper : IAdlsClient
    {

        public DataLakeServiceClient AdlsClient { get; private set; }

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

            JToken adlsAccountToken = adlsClientToken.SelectToken("Account");
            JToken adlsTenantIdToken = adlsClientToken.SelectToken("TenantId");
            JToken adlsIngestionApplicationIdToken = adlsClientToken.SelectToken("IngestionApplicationId");
            JToken adlsIngestionApplicationSecretEnvironmentVariableToken = adlsClientToken.SelectToken("IngestionApplicationSecretEnvironmentVariable");
            JToken serviceUriToken = adlsClientToken.SelectToken("ServiceUri");
            JToken storageAccountNameToken = adlsClientToken.SelectToken("StorageAccountNameToken");
            JToken storageAccountKeyToken = adlsClientToken.SelectToken("storageAccountKeyToken");
            if (adlsIngestionApplicationIdToken.IsNullOrWhiteSpace() || adlsIngestionApplicationSecretEnvironmentVariableToken.IsNullOrWhiteSpace() || adlsAccountToken.IsNullOrWhiteSpace() || adlsTenantIdToken.IsNullOrWhiteSpace())
            {
                // Don't fail loading the function app environment if these variables are not provided. Instead don't initialize the ADLS client.
                // This way, we permit functions that don't depend on the ADLS client to still run without configuring the ADLS client.
                // Once the function loads, we will also log the fact that ADLS client is not initialized separately.
                return;
            }

            string adlsAccount = adlsAccountToken.Value<string>();
            string adlsTenantId = adlsTenantIdToken.Value<string>();
            string adlsIngestionApplicationId = adlsIngestionApplicationIdToken.Value<string>();
            string adlsIngestionApplicationSecret = Environment.GetEnvironmentVariable(adlsIngestionApplicationSecretEnvironmentVariableToken.Value<string>());
            Uri serviceUri = serviceUriToken.Value<Uri>();
            string storageAccountName = storageAccountNameToken.Value<string>();
            string storageAccountKey = storageAccountKeyToken.Value<string>();
            if (string.IsNullOrWhiteSpace(adlsIngestionApplicationSecret))
            {
                throw new FatalTerminalException($"For token '{adlsIngestionApplicationSecretEnvironmentVariableToken}', local.settings.json must provide an ADLS Ingestion Application Secret.");
            }

            this.AdlsClient = InitializeAdlsClient(serviceUri, storageAccountName, storageAccountKey);
        }

        private void LoadSettingsFromEnvironmentVariables()
        {
            string adlsAccount = Environment.GetEnvironmentVariable("AdlsAccount");
            if (string.IsNullOrWhiteSpace(adlsAccount))
            {
                adlsAccount = "1es-private-data-c14.azuredatalakestore.net";
            }

            string adlsTenantId = Environment.GetEnvironmentVariable("AdlsTenantId");
            if (string.IsNullOrWhiteSpace(adlsTenantId))
            {
                adlsTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            }

            string adlsIngestionApplicationId = Environment.GetEnvironmentVariable("AdlsIngestionApplicationId");
            string adlsIngestionApplicationSecret = Environment.GetEnvironmentVariable("AdlsIngestionApplicationSecret");

            if (string.IsNullOrWhiteSpace(adlsIngestionApplicationId) || string.IsNullOrWhiteSpace(adlsIngestionApplicationSecret))
            {
                // Don't fail loading the function app environment if these variables are not provided. Instead don't initialize the ADLS client.
                // This way, we permit functions that don't depend on the ADLS client to still run without configuring the ADLS client.
                // Once the function loads, we will also log the fact that ADLS client is not initialized separately.
                return;
            }

            Uri serviceUri = new Uri(Environment.GetEnvironmentVariable("AdlsServiceUri"));
            string storageAccountName = Environment.GetEnvironmentVariable("storageAccountName");
            string storageAccountKey = Environment.GetEnvironmentVariable("storageAccountKey");

            this.AdlsClient = InitializeAdlsClient(serviceUri, storageAccountName, storageAccountKey);
        }

        internal static DataLakeServiceClient InitializeAdlsClient(Uri serviceUri, string storageAccountName, string storageAccountKey)
        {

            StorageSharedKeyCredential sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, storageAccountKey);

            DataLakeServiceClient serviceClient = new DataLakeServiceClient(serviceUri, sharedKeyCredential);

            // Marcel: ProcCount * 8 is usually the recommended number of threads to be used without deprecation of performance to to overscheduling and preemption. It supposed to account for usage and IO completion waits.
            return serviceClient;
        }
    }
}
