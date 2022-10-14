// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Data.Tables;
using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public interface ITableEntityWithContext : ITableEntity
    {
        void AddContext(string propertyName, string propertyValue);
        Dictionary<string, string> GetContext();
    }
}
