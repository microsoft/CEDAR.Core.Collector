// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public abstract class TableEntityWithContext : TableEntity
    {
        private readonly Dictionary<string, string> context;

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
