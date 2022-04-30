using System;
using System.Diagnostics;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class TelemetryMetric<T>
    {
        private string name;

        private TelemetryMetric(string name)
        {
            this.name = name;
        }

        private void Add(T value, TagList tags)
        {

        }
        public static void EmitMetric(TelemetryMetric<T> metric, T value, TagList tags = new TagList())
        {
            //Meter.CreateCounter<T>(name).AddWithDefaultTags(value, tags);
        }

        public static TelemetryMetric<long> RecordCount = new TelemetryMetric<long>("RecordCount");
        public static TelemetryMetric<long> HeartBeat = new TelemetryMetric<long>("Heartbeat");
        public static TelemetryMetric<double> RateLimit = new TelemetryMetric<double>("RateLimitDelay");
    }
}
