// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.IO;
using Microsoft.CloudMine.Core.Telemetry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CloudMine.Core.Collectors.Collector
{
    /// <summary>
    /// StatsTracker is a nice little tool that helps you wrap a function execution so that you get regular (controlled by the refreshFrequency) updates on the function progress,
    /// such as the number of successful and failing web reqeusts, and the amount of data that is collected (per record type).
    /// When run in "Release" mode, the StatsTracker logs the data to the associated AI instance as a custom event. When run in "Debug" mode (e.g., locally), StatsTracker also prints
    /// this information on the console as long as the TelemetryClient is associated with the ILogger at the beginning of the function execution.
    /// 
    /// You can adopt the following pattern to use the StatsTracker:
    /// 
    /// StatsTracker statsTracker = null;
    /// try
    /// {
    ///     // Function code to setup your logic.
    ///     ...
    ///     ITelemetryClient telemetryClient = // create a telemetry client. Don't forget to pass ILogger from the function is you want to see StatsTracker details locally.
    ///     IWebRequestStatsTracker webRequestStatsTracker = // create a web request stats tracker.
    ///     RecordWriterCore<FunctionContext> recordWriter = // create a record writer.
    ///     statsTracker = new StatsTracker(telemetryClient, webRequestStatsTracker, recordWriter, TimeSpan.FromSeconds(10));
    ///     ...
    ///     // More logic to run the function
    /// }
    /// finally
    /// {
    ///     statsTracker?.Stop();
    /// }

    /// </summary>
    public class StatsTracker
    {
        private readonly ITelemetryClient telemetryClient;
        private readonly IWebRequestStatsTracker webRequestStatsTracker;
        private readonly IRecordStatsTracker recordStatsTracker;

        private readonly Timer statsTracker;

        public StatsTracker(ITelemetryClient telemetryClient, IWebRequestStatsTracker webRequestStatsTracker, TimeSpan refreshFrequncy)
            : this(telemetryClient, webRequestStatsTracker, recordStatsTracker: null, refreshFrequncy)
        {
        }

        public StatsTracker(ITelemetryClient telemetryClient, IWebRequestStatsTracker webRequestStatsTracker, IRecordStatsTracker recordStatsTracker, TimeSpan refreshFrequncy)
        {
            this.telemetryClient = telemetryClient;
            this.webRequestStatsTracker = webRequestStatsTracker;
            this.recordStatsTracker = recordStatsTracker;

            this.statsTracker = new Timer(LogStats, state: null, dueTime: TimeSpan.Zero, period: refreshFrequncy);
        }

        private void LogStats(object state)
        {
            int successfulRequestCount = this.webRequestStatsTracker.SuccessfulRequestCount;
            int failedRequestCount = this.webRequestStatsTracker.FailedRequestCount;

            string webRequestSummary = $"Web request summary: Successful = {successfulRequestCount}, Failed = {failedRequestCount}";
#if DEBUG
            this.telemetryClient.LogInformation(webRequestSummary);
#endif

            Dictionary<string, string> properties = new Dictionary<string, string>()
            {
                { "SuccessfulRequestCount", successfulRequestCount.ToString() },
                { "FailedRequestCount", failedRequestCount.ToString() },
            };

            if (this.recordStatsTracker != null)
            {
                ConcurrentDictionary<string, int> recordStats = this.recordStatsTracker.RecordStats;
                List<string> recordTypes = new List<string>(recordStats.Keys);
                recordTypes.Sort();

                string recordCountSummary = $"Record count summary: ";
                int totalRecordCount = 0;
                foreach (string recordType in recordTypes)
                {
                    int recordCount = recordStats[recordType];
                    totalRecordCount += recordCount;
                    properties.Add($"{recordType}.RecordCount", recordCount.ToString());

                    recordCountSummary += $"{recordType} = {recordCount}, ";
                }

                properties.Add("TotalRecordCount", totalRecordCount.ToString());
                recordCountSummary += $"Total = {totalRecordCount}";

#if DEBUG
                this.telemetryClient.LogInformation(recordCountSummary);
#endif
            }

            this.telemetryClient.TrackEvent("CollectionStats", properties);
        }

        public void Stop()
        {
            // Do a final update regardless before stopping the time.
            this.LogStats(state: null);
            this.statsTracker.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
}
