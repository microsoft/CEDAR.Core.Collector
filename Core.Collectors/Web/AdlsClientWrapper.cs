// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.DataLake.Store;
using Microsoft.CloudMine.Core.Collectors.Error;
using Microsoft.CloudMine.Core.Collectors.Utility;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public interface IAdlsClient
    {
        public AdlsClient AdlsClient { get; }
    }

    public class AdlsClientWrapper : IAdlsClient
    {
        public static readonly Uri AdlTokenAudience = new Uri(@"https://datalake.azure.net/");
        public const string AdlsAccount = "1es-private-data-c14.azuredatalakestore.net";
        public const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47"; // Microsoft tenant

        public AdlsClient AdlsClient { get; private set; }

        // This is the old AdlsClientWrapper API.
        // The old API will be deprecated soon, but to protect our jobs from failing, we need to support both the old API and new API.
        public AdlsClientWrapper()
        {
            string clientId = Environment.GetEnvironmentVariable("AdlsIngestionApplicationId");
            string secretKey = Environment.GetEnvironmentVariable("AdlsIngestionApplicationSecret");

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secretKey))
            {
                // Don't fail loading the function app environment if these variables are not provided. Instead don't initialize the ADLS client.
                // This way, we permit functions that don't depend on the ADLS client to still run without configuring the ADLS client.
                // Once the function loads, we will also log the fact that ADLS client is not initialized separately.
                return;
            }

            ActiveDirectoryServiceSettings serviceSettings = ActiveDirectoryServiceSettings.Azure;
            serviceSettings.TokenAudience = AdlTokenAudience;
            ServiceClientCredentials adlCreds = ApplicationTokenProvider.LoginSilentAsync(TenantId, clientId, secretKey, serviceSettings).GetAwaiter().GetResult();

            // Marcel: ProcCount * 8 is usually the recommended number of threads to be used without deprecation of performance to to overscheduling and preemption. It supposed to account for usage and IO completion waits.
            this.AdlsClient = AdlsClient.CreateClient(AdlsAccount, adlCreds, Environment.ProcessorCount * 8);
        }

        public AdlsClientWrapper(string settings)
        {
            if (settings == null)
            {
                // When the global settings string is null, we are using the old AdlsClientWrapper API.
                // The old API had to be moved from a constructor to a private method because the Azure Function magic in builder services is sometimes calling the new API
                // even with an old version of CEDAR/GitHub and ADO (this is probably due to having a preference for the constructor with a parameter).
                this.InvokeOldAdlsClientWrapperApi();
                return;
            }

            JObject config = JObject.Parse(settings);
            JToken clientIdToken = config.SelectToken("AdlsIngestionApplicationId");
            JToken secretKeyEnvironmentVariableToken = config.SelectToken("AdlsIngestionApplicationSecretEnvironmentVariable");
            if (clientIdToken.IsNullOrWhiteSpace() || secretKeyEnvironmentVariableToken.IsNullOrWhiteSpace())
            {
                // Don't fail loading the function app environment if these variables are not provided. Instead don't initialize the ADLS client.
                // This way, we permit functions that don't depend on the ADLS client to still run without configuring the ADLS client.
                // Once the function loads, we will also log the fact that ADLS client is not initialized separately.
                return;
            }

            string clientId = clientIdToken.Value<string>();
            string secretKey = Environment.GetEnvironmentVariable(secretKeyEnvironmentVariableToken.Value<string>());
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new FatalTerminalException($"For token '{secretKeyEnvironmentVariableToken}', local.settings.json must provide an ADLS secret key.");
            }

            ActiveDirectoryServiceSettings serviceSettings = ActiveDirectoryServiceSettings.Azure;
            serviceSettings.TokenAudience = AdlTokenAudience;
            ServiceClientCredentials adlCreds = ApplicationTokenProvider.LoginSilentAsync(TenantId, clientId, secretKey, serviceSettings).GetAwaiter().GetResult();

            // Marcel: ProcCount * 8 is usually the recommended number of threads to be used without deprecation of performance to to overscheduling and preemption. It supposed to account for usage and IO completion waits.
            this.AdlsClient = AdlsClient.CreateClient(AdlsAccount, adlCreds, Environment.ProcessorCount * 8);
        }

        private void InvokeOldAdlsClientWrapperApi()
        {
            string clientId = Environment.GetEnvironmentVariable("AdlsIngestionApplicationId");
            string secretKey = Environment.GetEnvironmentVariable("AdlsIngestionApplicationSecret");

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secretKey))
            {
                // Don't fail loading the function app environment if these variables are not provided. Instead don't initialize the ADLS client.
                // This way, we permit functions that don't depend on the ADLS client to still run without configuring the ADLS client.
                // Once the function loads, we will also log the fact that ADLS client is not initialized separately.
                return;
            }

            ActiveDirectoryServiceSettings serviceSettings = ActiveDirectoryServiceSettings.Azure;
            serviceSettings.TokenAudience = AdlTokenAudience;
            ServiceClientCredentials adlCreds = ApplicationTokenProvider.LoginSilentAsync(TenantId, clientId, secretKey, serviceSettings).GetAwaiter().GetResult();

            // Marcel: ProcCount * 8 is usually the recommended number of threads to be used without deprecation of performance to to overscheduling and preemption. It supposed to account for usage and IO completion waits.
            this.AdlsClient = AdlsClient.CreateClient(AdlsAccount, adlCreds, Environment.ProcessorCount * 8);
        }
    }
}
