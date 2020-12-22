// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public interface IRecordWriter : IRecordStatsTracker, IDisposable
    {
        IEnumerable<string> OutputPaths { get; }
        void SetOutputPathPrefix(string outputPathPrefix);
        Task FinalizeAsync();
        Task WriteLineAsync(string content);
        Task WriteRecordAsync(JObject record, RecordContext context);
        Task NewOutputAsync(string outputSuffix, int fileIndex = 0);
    }

    public enum RecordWriterType
    {
        AzureDataLakeStorageV1,
        AzureBlob,
        SplitAzureBlob,
    }
}
