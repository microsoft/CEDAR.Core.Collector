// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure;
using Azure.Data.Tables;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public abstract class TableEntityWithContext : ITableEntity
    {
        private readonly Dictionary<string, string> context;

        public TableEntityWithContext()
        {
            context = new Dictionary<string, string>();
            AddContext("ObjectType", GetType().ToString());
        }

        public Dictionary<string, string> GetContext()
        {
            return context;
        }

        public void AddContext(string propertyName, string propertyValue)
        {
            context.Add(propertyName, propertyValue);
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
