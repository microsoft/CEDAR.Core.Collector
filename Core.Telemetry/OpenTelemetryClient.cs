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
        private const string SUBSCRIPTION_KEY = "Collectors";

        private static readonly Meter Meter = new Meter(SUBSCRIPTION_KEY, "1.0");
        private static readonly ActivitySource ActivitySource = new ActivitySource(SUBSCRIPTION_KEY);

        
        private static readonly string appEnv = OpenTelemetryHelpers.GetEnvironmentVariableWithDefault("AppEnv", defaultValue: "*");

        private readonly ILoggerFactory loggerFactory;
        private readonly TracerProvider tracerProvider;
        private readonly MeterProvider meterProvider;

        

        public OpenTelemetryClient()
        {
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

        public static Activity GetActivity(string name)
        {
            return ActivitySource.CreateActivity(name, ActivityKind.Internal).AddDefaultTags();
        }

        public static void EmitMetric<T>(string name, T value, TagList tags = new TagList()) where T : struct
        {
            Meter.CreateCounter<T>(name).AddWithDefaultTags(value, tags);
        }
    }

    public static class OpenTelemetryHelpers
    {

        public static readonly string Product = GetEnvironmentVariableWithDefault("Product");
        public static readonly string Service = GetEnvironmentVariableWithDefault("Service");
        public static readonly string AppEnvironment = GetEnvironmentVariableWithDefault("Environment", defaultValue: "*");
        public static readonly string Deployment = GetEnvironmentVariableWithDefault("Deployment", defaultValue: "*");
        public static readonly string Region = GetEnvironmentVariableWithDefault("Region", defaultValue: "*");

        public static Activity AddDefaultTags(this Activity activity)
        {
            activity.AddTag("Product", Product);
            activity.AddTag("Service", Service);
            activity.AddTag("Environment", AppEnvironment);
            activity.AddTag("Deployment", Deployment);
            activity.AddTag("Region", Region);

            return activity;
        }

        public static void AddWithDefaultTags<T>(this Counter<T> counter, T value, TagList tags) where T : struct
        {
            tags.Add("Product", Product);
            tags.Add("Service", Service);
            tags.Add("Environment", AppEnvironment);
            tags.Add("Deployment", Deployment);
            tags.Add("Region", Region);
            counter.Add(value, tags);
        }

        public static string GetEnvironmentVariableWithDefault(string variable, string defaultValue = null)
        {
            try
            {
                return Environment.GetEnvironmentVariable(variable);
            }
            catch (ArgumentNullException)
            {
                if (defaultValue == null)
                {
                    throw;
                }

                return defaultValue;
            }
        }
    }
}
