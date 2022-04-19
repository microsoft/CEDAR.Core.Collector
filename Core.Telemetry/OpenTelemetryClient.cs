using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Exporter.Geneva;
using System.Collections.Generic;
using System;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class OpenTelemetryClient : ILogger
    {
        private ILogger logger;

        public OpenTelemetryClient()
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(loggerOptions =>
            {
                loggerOptions.AddGenevaLogExporter(exporterOptions =>
                {
                    exporterOptions.ConnectionString = "EtwSession=OpenTelemetry";

                    exporterOptions.PrepopulatedFields = new Dictionary<string, object>
                    {
                        ["cloud.role"] = "BusyWorker",
                        ["cloud.roleInstance"] = "CY1SCH030021417",
                        ["cloud.roleVer"] = "9.0.15289.2",
                    };
                });
            }));

            this.logger = loggerFactory.CreateLogger<OpenTelemetryClient>();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logger.IsEnabled(logLevel);
        }

        public void Log(string message)
        {
            this.logger.LogInformation(message);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
