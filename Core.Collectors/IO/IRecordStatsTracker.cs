// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public interface IRecordStatsTracker
    {
        ConcurrentDictionary<string, int> RecordStats { get; }
    }
}
