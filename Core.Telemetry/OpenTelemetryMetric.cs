// Copyright (c) Microsoft Corporation. All rights reserved.

using OpenTelemetry;
using OpenTelemetry.Exporter.Geneva;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class OpenTelemetryMetric
    {
        public static TelemetryMetric<long> RecordCounter;
        public static TelemetryMetric<long> HeartbeatCounter;
        public static TelemetryMetric<double> RateLimitDelay;

        private const string SUBSCRIPTION_KEY = "Metrics";

        public static readonly Meter OpenTelemetryMeter;
        private static readonly MeterProvider MeterProvider;

        static OpenTelemetryMetric()
        {
            OpenTelemetryMeter = new Meter(SUBSCRIPTION_KEY, "1.0");
            MeterProvider = BuildMeterProvider();
            RecordCounter = new TelemetryMetric<long>("RecordCounter");
            HeartbeatCounter = new TelemetryMetric<long>("HeartbeatCounter");
            RateLimitDelay = new TelemetryMetric<double>("RateLimitDelay");
        }

        public static void Dispose()
        {
            MeterProvider.Dispose();
        }

        private static MeterProvider BuildMeterProvider()
        {
            MeterProvider meterProvider;

            switch (OpenTelemetryHelpers.OpenTelemetryExporter)
            {
                case OpenTelemetryHelpers.GenevaExporterName:
                    string connectionString = $"Account={OpenTelemetryHelpers.Product};Namespace={OpenTelemetryHelpers.Service}";

                    meterProvider = Sdk.CreateMeterProviderBuilder().AddMeter(SUBSCRIPTION_KEY).AddGenevaMetricExporter(options =>
                    {
                        options.ConnectionString = connectionString;
                    }).Build();

                    break;
                default:
                    meterProvider = Sdk.CreateMeterProviderBuilder().AddMeter(SUBSCRIPTION_KEY).AddConsoleExporter().Build();
                    break;
            }

            return meterProvider;
        }
    }

    public class TelemetryMetric<T> where T : struct
    {
        private Counter<T> counter;

        public TelemetryMetric(string name)
        {
            this.counter = OpenTelemetryMetric.OpenTelemetryMeter.CreateCounter<T>(name);
        }

        public void Add(T value, TagList tags = new TagList())
        {
            this.counter.AddWithDefaultTags(value, tags);
        }
    }
}
