// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public class TableEntityWithContext : ITableEntity
    {
        private readonly Dictionary<string, string> context;

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public TableEntityWithContext()
        {
            this.context = new Dictionary<string, string>();
            this.AddContext("ObjectType", this.GetType().ToString());
        }

        public Dictionary<string, string> GetContext()
        {
            return this.context;
        }

        public void AddContext(string propertyName, string propertyValue)
        {
            context.Add(propertyName, propertyValue);
        }
    }
}
