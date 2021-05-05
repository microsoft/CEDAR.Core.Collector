// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.DataLake.Store;
using System;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public interface IAdlsClient2
    {
        public AdlsClient AdlsClient { get; }
    }

    public class AdlsClientWrapper2 : IAdlsClient2
    {
        public static readonly Uri AdlTokenAudience = new Uri(@"https://datalake.azure.net/");

        public AdlsClient AdlsClient { get; private set; }

        public AdlsClientWrapper2()
        {
            string adlsAccount = Environment.GetEnvironmentVariable("AdlsAccount2");
            string adlsTenantId = Environment.GetEnvironmentVariable("AdlsTenantId2");
            string adlsIngestionApplicationId = Environment.GetEnvironmentVariable("AdlsIngestionApplicationId2");
            string adlsIngestionApplicationSecret = Environment.GetEnvironmentVariable("AdlsIngestionApplicationSecret2");

            if (string.IsNullOrWhiteSpace(adlsIngestionApplicationId) || string.IsNullOrWhiteSpace(adlsIngestionApplicationSecret) || string.IsNullOrWhiteSpace(adlsAccount) || string.IsNullOrWhiteSpace(adlsTenantId))
            {
                // Don't fail loading the function app environment if these variables are not provided. Instead don't initialize the ADLS client.
                // This way, we permit functions that don't depend on the ADLS client to still run without configuring the ADLS client.
                // Once the function loads, we will also log the fact that ADLS client is not initialized separately.
                return;
            }

            this.AdlsClient = AdlsClientWrapper.InitializeAdlsClient(adlsAccount, adlsTenantId, adlsIngestionApplicationId, adlsIngestionApplicationSecret);
        }
    }
}
