﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    [Serializable]
    public class RecordWithContext
    {
        public JObject Record { get; set; }
        public RecordContext Context { get; set; }

        public RecordWithContext()
        {
        }

        public RecordWithContext(JObject record, RecordContext context)
        {
            this.Record = record;
            this.Context = context;
        }
    }
}
