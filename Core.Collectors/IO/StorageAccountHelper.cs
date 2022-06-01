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
        private static readonly string EndPointSuffix = "core.windows.net";
        public static CloudStorageAccount GetStorageAccountUsingCS(string storageConnectionEnvironmentVariable)
        {
            string stagingBlobConnectionString = Environment.GetEnvironmentVariable(storageConnectionEnvironmentVariable);
            return CloudStorageAccount.Parse(stagingBlobConnectionString);
        }

        public async static Task<CloudStorageAccount> GetStorageAccountUsingMsi(string storageAccountNameEnvironmentVariable = "StorageAccountName", bool isQueue = false)
        {
            string storageAccountName = Environment.GetEnvironmentVariable(storageAccountNameEnvironmentVariable);
            string resource = isQueue ? GetQueueResource(storageAccountName) : GetBlobResource(storageAccountName);
            StorageCredentials creds = await GetStorageCredentails(resource).ConfigureAwait(false);
            CloudStorageAccount storageAccount = new CloudStorageAccount(creds, storageAccountName, EndPointSuffix, true);
            return storageAccount;            
        }

        private async static Task<StorageCredentials> GetStorageCredentails(string resource)
        {
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            string token = await azureServiceTokenProvider.GetAccessTokenAsync(resource).ConfigureAwait(false);
            TokenCredential tokenCredential = new TokenCredential(token);
            return new StorageCredentials(tokenCredential);
        }

        public static string GetBlobResource(string storageAccountName)
        {
            return $"https://{storageAccountName}.blob.core.windows.net/";
        }

        public static string GetQueueResource(string storageAccountName)
        {
            return $"https://{storageAccountName}.queue.core.windows.net/";
        }
    }
}
