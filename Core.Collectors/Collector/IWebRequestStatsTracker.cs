// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.CloudMine.Core.Collectors.Collector
{
    /// <summary>
    /// Abstracts the capability of tracking the number of successful and failed web requests such that an HttpClient can be used in a StatsTracker.
    /// </summary>
    public interface IWebRequestStatsTracker
    {
        int SuccessfulRequestCount { get; }
        int FailedRequestCount { get; }
    }
}
