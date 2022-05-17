using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using System;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public static class StorageAccountHelper
    {
        private static readonly string endPointSuffix = "core.windows.net";
        public static CloudStorageAccount GetStorageAccount(string storageConnectionEnvironmentVariable)
        {
            string stagingBlobConnectionString = Environment.GetEnvironmentVariable(storageConnectionEnvironmentVariable);
            return CloudStorageAccount.Parse(stagingBlobConnectionString);
        }

        public async static Task<CloudStorageAccount> GetStorageAccountUsingMsi(string storageAccountNameEnvironmentVariable = "StorageAccountName", bool isQueue = false)
        {
            string storageAccountName = Environment.GetEnvironmentVariable(storageAccountNameEnvironmentVariable);
            var resource = isQueue ? AzureStorageResourceHelper.GetQueueResource(storageAccountName) : AzureStorageResourceHelper.GetBlobResource(storageAccountName);
            var creds = await GetStorageCredentails(resource);
            CloudStorageAccount storageAccount = new CloudStorageAccount(creds, storageAccountName, endPointSuffix, true);
            return storageAccount;            
        }

        private async static Task<StorageCredentials> GetStorageCredentails(string resource)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var token = await azureServiceTokenProvider.GetAccessTokenAsync(resource);
            TokenCredential tokenCredential = new TokenCredential(token);
            return new StorageCredentials(tokenCredential);
        }
    }
}
