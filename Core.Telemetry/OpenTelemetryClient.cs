using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter.Geneva;
using OpenTelemetry.Metrics;
using System.Diagnostics;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class OpenTelemetryClient
    {
        private ILogger logger;
        private MeterProvider meterProvider;
        private static readonly Meter MyMeter = new Meter("Microsoft.CloudMine.Collectors", "1.0");
        private static readonly Counter<long> MyHeartbeatCounter = MyMeter.CreateCounter<long>("MyHeartbeatCounter");

        public OpenTelemetryClient()
        {
            this.BuildLogger();
            this.BuildMetricsExporter();
        }

        private void BuildLogger()
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(loggerOptions =>
            {
                loggerOptions.AddGenevaLogExporter(exporterOptions =>
                {
                    exporterOptions.ConnectionString = "EtwSession=OpenTelemetry";
                    exporterOptions.PrepopulatedFields = new Dictionary<string, object>
                    {
                        //["cloud.role"] = "Collectors",
                        //["cloud.roleInstance"] = "CY1SCH030021417",
                        //["cloud.roleVer"] = "9.0.15289.2",
                    };
                });
            }));

            this.logger = loggerFactory.CreateLogger<OpenTelemetryClient>();
        }

        private void BuildMetricsExporter()
        {
            
        }

        public void Log(string message)
        {
            this.logger.LogInformation(message);
        }

        public void EmitMetric(long num)
        {
            this.meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("Microsoft.CloudMine.*")
            .AddGenevaMetricExporter(options =>
            {
                options.ConnectionString = "Account=CloudMine;Namespace=CmCollectors";
            })
            .Build();

            TagList tags = new TagList();
            tags.Add("text", "hello world");
            MyHeartbeatCounter.Add(num, tags);
        }

    }
}
