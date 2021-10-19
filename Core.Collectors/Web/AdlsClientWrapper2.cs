// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Storage;
using Azure.Storage.Files.DataLake;
using System;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public interface IAdlsClient2
    {
        public DataLakeServiceClient serviceClient { get; }
    }

    public class AdlsClientWrapper2 : IAdlsClient2
    {
        public static readonly Uri serviceUri = new Uri(@"https://datalake.azure.net/");

        public DataLakeServiceClient serviceClient { get; private set; }

        public AdlsClientWrapper2()
        {
            string storageAccountName = Environment.GetEnvironmentVariable("StorageAccountName");
            string storageAccountKey = Environment.GetEnvironmentVariable("StorageAccountKey");

            StorageSharedKeyCredential sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, storageAccountKey);

            // Create DataLakeServiceClient using StorageSharedKeyCredentials
            this.serviceClient = new DataLakeServiceClient(serviceUri, sharedKeyCredential);
        }
    }
}
