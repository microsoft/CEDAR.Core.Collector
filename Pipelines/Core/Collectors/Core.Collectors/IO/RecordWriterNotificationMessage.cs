// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    [Serializable]
    public class RecordWriterNotificationMessage
    {
        public string ContainerName { get; set; }
        public string BlobPath { get; set; }
        public long RecordCount { get; set; }
        public DateTime MinCollectionDateUtc { get; set; }
        public DateTime MaxCollectionDateUtc { get; set; }
        public string RecordType { get; set; }
    }
}
