// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public class WatermarkTableEntity : TableEntityWithContext
    {
        public string Watermark { get; set; }
        public string Identifier { get; set; }
        public string Type { get; set; }

        // Used for serialization.
        public WatermarkTableEntity()
        {
        }

        // Used only to retrieve (lookup) entries.
        public WatermarkTableEntity(string identifier, string type)
            : this(identifier, type, string.Empty)
        {
        }

        public WatermarkTableEntity(string identifier, string type, string watermark)
        {
            this.PartitionKey = identifier;
            this.RowKey = type;

            this.Identifier = identifier;
            this.Type = type;
            this.Watermark = watermark;

            this.AddContext("Type", this.Type);
            this.AddContext("Identifier", this.Identifier);
            this.AddContext("Watermark", this.Watermark);
        }
    }
}
