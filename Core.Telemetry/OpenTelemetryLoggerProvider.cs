// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.CloudMine.Core.Telemetry
{
    // OpenTelemetry Los documentation: https://eng.ms/docs/products/geneva/collect/instrument/opentelemetrydotnet/otel-logs
    public class OpenTelemetryLoggerProvider : ILoggerProvider
    {
        private readonly ILoggerFactory loggerFactory;

        public OpenTelemetryLoggerProvider()
        {
            switch (OpenTelemetryHelpers.OpenTelemetryExporter)
            {
                case OpenTelemetryHelpers.GenevaExporterName:
                    this.loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.AddOpenTelemetry(loggerOptions =>
                        {
                            loggerOptions.IncludeFormattedMessage = true;
                            loggerOptions.AddGenevaLogExporter(exporterOptions =>
                            {
                                exporterOptions.ConnectionString = "EtwSession=OpenTelemetry";
                            });
                        });
                    });

                    break;

                case OpenTelemetryHelpers.ConsoleExporterName:
                    this.loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.AddOpenTelemetry(loggerOptions => loggerOptions.AddConsoleExporter());
                    });

                    break;

                default:
                    this.loggerFactory = LoggerFactory.Create(builder => { });
                    break;
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return this.loggerFactory.CreateLogger(categoryName);
        }

        public void Dispose()
        {
            this.loggerFactory.Dispose();
            OpenTelemetryMetric.Dispose();
            OpenTelemetryTracer.Dispose();
        }
    }
}
