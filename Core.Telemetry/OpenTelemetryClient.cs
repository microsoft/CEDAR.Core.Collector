using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter.Geneva;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class OpenTelemetryClient : ILoggerProvider
    {
        private const string SUBSCRIPTION_KEY = "Telemetry";

        private static readonly Meter Meter = new Meter(SUBSCRIPTION_KEY, "1.0");
        private static readonly ActivitySource ActivitySource = new ActivitySource(SUBSCRIPTION_KEY);

        private readonly ILoggerFactory loggerFactory;
        private readonly TracerProvider tracerProvider;
        private readonly MeterProvider meterProvider;

        public OpenTelemetryClient()
        {
            string appEnv = OpenTelemetryHelpers.GetEnvironmentVariableWithDefault("AppEnv", defaultValue: "*");

            if (appEnv.Equals("Production"))
            {
                string connectionString = $"Account={OpenTelemetryHelpers.Product};Namespace={OpenTelemetryHelpers.Service}";
                this.tracerProvider = Sdk.CreateTracerProviderBuilder().SetSampler(new AlwaysOnSampler()).AddSource(SUBSCRIPTION_KEY).AddGenevaTraceExporter(options => { options.ConnectionString = "EtwSession=OpenTelemetry"; }).Build();
                this.meterProvider = Sdk.CreateMeterProviderBuilder().AddMeter(SUBSCRIPTION_KEY).AddGenevaMetricExporter(options => { options.ConnectionString = connectionString; }).Build();
                this.loggerFactory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(loggerOptions => loggerOptions.AddGenevaLogExporter(exporterOptions => { exporterOptions.ConnectionString = "EtwSession=OpenTelemetry"; })));
            }
            else
            {
                this.tracerProvider = Sdk.CreateTracerProviderBuilder().SetSampler(new AlwaysOnSampler()).AddSource(SUBSCRIPTION_KEY).AddConsoleExporter().Build();
                this.meterProvider = Sdk.CreateMeterProviderBuilder().AddMeter(SUBSCRIPTION_KEY).AddConsoleExporter().Build();
                this.loggerFactory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(loggerOptions => loggerOptions.AddConsoleExporter()));
            }
        }

        public static Activity GetActivity(string name)
        {
            return ActivitySource.CreateActivity(name, ActivityKind.Internal).AddDefaultTags();
        }

        public static void EmitMetric<T>(string name, T value, TagList tags = new TagList()) where T : struct
        {
            Meter.CreateCounter<T>(name).AddWithDefaultTags(value, tags);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return this.loggerFactory.CreateLogger(categoryName);
        }

        public void Dispose()
        {
            this.tracerProvider.Dispose();
            this.meterProvider.Dispose();
            this.loggerFactory.Dispose();
        }
    }
}
