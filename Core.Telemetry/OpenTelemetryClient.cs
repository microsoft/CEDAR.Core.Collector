using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter.Geneva;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class OpenTelemetryClient : ILoggerProvider
    {
        private const string SERVICE_NAME = "Microsoft.CloudMine.Collectors";

        public static readonly Meter Meter = new Meter(SERVICE_NAME, "1.0");
        public static readonly ActivitySource ActivitySource = new ActivitySource(SERVICE_NAME);

        private readonly ILoggerFactory loggerFactory;
        private readonly TracerProvider tracerProvider;
        private readonly MeterProvider meterProvider;

        public OpenTelemetryClient()
        {
            string appEnv = Environment.GetEnvironmentVariable("AppEnv");
            if (appEnv.Equals("Production"))
            {
                this.tracerProvider = Sdk.CreateTracerProviderBuilder().SetSampler(new AlwaysOnSampler()).AddSource(SERVICE_NAME).AddGenevaTraceExporter(options => { options.ConnectionString = "EtwSession=OpenTelemetry"; }).Build();
                this.meterProvider = Sdk.CreateMeterProviderBuilder().AddMeter(SERVICE_NAME).AddGenevaMetricExporter(options => { options.ConnectionString = "Account=CloudMine;Namespace=CmCollectors"; }).Build();
                this.loggerFactory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(loggerOptions => loggerOptions.AddGenevaLogExporter(exporterOptions => { exporterOptions.ConnectionString = "EtwSession=OpenTelemetry"; })));
            }
            else
            {
                this.tracerProvider = Sdk.CreateTracerProviderBuilder().SetSampler(new AlwaysOnSampler()).AddSource(SERVICE_NAME).AddConsoleExporter().Build();
                this.meterProvider = Sdk.CreateMeterProviderBuilder().AddMeter(SERVICE_NAME).AddConsoleExporter().Build();
                this.loggerFactory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(loggerOptions => loggerOptions.AddConsoleExporter()));
            }
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
