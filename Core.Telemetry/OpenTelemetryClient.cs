using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Exporter.Geneva;
using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class OpenTelemetryClient
    {
        private ILogger logger;

        public OpenTelemetryClient(string connectionString)
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

        public void Log(string message)
        {
            this.logger.LogInformation(message);
        }
    }
}
