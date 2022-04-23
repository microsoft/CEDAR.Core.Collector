using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter.Geneva;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using OpenTelemetry.Trace;
using System;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class OpenTelemetryClient : ILoggerProvider, IDisposable
    {
        private const string SERVICE_NAME = "Microsoft.CloudMine.Collectors";
        private static readonly Meter Meter = new Meter(SERVICE_NAME, "1.0");

        public static readonly ActivitySource ActivitySource = new ActivitySource(SERVICE_NAME);
        public static readonly Counter<long> RecordCounter = Meter.CreateCounter<long>("RecordCounter");
        public static readonly Counter<double> RateLimitDelayCounter = Meter.CreateCounter<double>("RateLimitDelayCounter");

        private readonly ILoggerFactory loggerFactory;
        private readonly TracerProvider tracerProvider;
        private readonly MeterProvider meterProvider;

        public OpenTelemetryClient()
        {
            this.tracerProvider = Sdk.CreateTracerProviderBuilder().SetSampler(new AlwaysOnSampler()).AddSource(SERVICE_NAME).AddGenevaTraceExporter(options => { options.ConnectionString = "EtwSession=OpenTelemetry"; }).Build();
            this.meterProvider = Sdk.CreateMeterProviderBuilder().AddMeter(SERVICE_NAME).AddGenevaMetricExporter(options => { options.ConnectionString = "Account=CloudMine;Namespace=CmCollectors"; }).Build();
            this.loggerFactory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(loggerOptions => { loggerOptions.AddGenevaLogExporter(exporterOptions => { exporterOptions.ConnectionString = "EtwSession=OpenTelemetry"; }); }));
        }

        public void Dispose()
        {
            this.tracerProvider.Dispose();
            this.meterProvider.Dispose();
            this.loggerFactory.Dispose();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return this.loggerFactory.CreateLogger(categoryName);
        }
    }
}
