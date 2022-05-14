using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public static class AzureStorageResourceHelper
    {
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
