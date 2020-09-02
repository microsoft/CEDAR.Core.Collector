// Copyright (c) Microsoft Corporation. All rights reserved.

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    [Serializable]
    public class RecordContext
    {
        public string RecordType { get; set; }
        public Dictionary<string, JToken> AdditionalMetadata { get; set; }
        public bool MetadataAugmented { get; set; } = false;

        public RecordContext()
        { 
        }

        public RecordContext(string recordType)
            : this(recordType, new Dictionary<string, JToken>())
        { 
        }

        public RecordContext(string recordType, Dictionary<string, JToken> additionalMetadata)
        {
            this.RecordType = recordType;
            this.AdditionalMetadata = additionalMetadata;
        }
    }
}
