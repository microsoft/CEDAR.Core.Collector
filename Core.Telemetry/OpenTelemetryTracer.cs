using OpenTelemetry;
using OpenTelemetry.Exporter.Geneva;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class OpenTelemetryTracer
    {
        private const string SUBSCRIPTION_KEY = "Traces";
        private static readonly TracerProvider TracerProvider = BuildTracerProvider();
        private static readonly ActivitySource ActivitySource = new ActivitySource(SUBSCRIPTION_KEY);

        public static Activity GetActivity(OpenTelemetryTrace trace)
        {
            return ActivitySource.CreateActivity(trace.Name, ActivityKind.Internal).AddDefaultTags();
        }

        public static void Dispose()
        {
            TracerProvider.Dispose();
        }

        private static TracerProvider BuildTracerProvider()
        {
            TracerProvider tracerProvider;

            switch (OpenTelemetryHelpers.OpenTelemetryExporter)
            {
                case OpenTelemetryHelpers.GenevaExporterName:
                    tracerProvider = Sdk.CreateTracerProviderBuilder().SetSampler(new AlwaysOnSampler()).AddSource(SUBSCRIPTION_KEY).AddGenevaTraceExporter(options => { options.ConnectionString = "EtwSession=OpenTelemetry"; }).Build();
                    break;
                default:
                    tracerProvider = Sdk.CreateTracerProviderBuilder().SetSampler(new AlwaysOnSampler()).AddSource(SUBSCRIPTION_KEY).AddConsoleExporter().Build();
                    break;
            }

            return tracerProvider;
        }

        public class OpenTelemetryTrace
        {
            public static OpenTelemetryTrace FunctionInvocation = new OpenTelemetryTrace("FunctionInvocation");
            public static OpenTelemetryTrace ProccessCollectionNode = new OpenTelemetryTrace("ProccessCollectionNode");
            public static OpenTelemetryTrace Heartbeat = new OpenTelemetryTrace("Heartbeat");
            public static OpenTelemetryTrace Request = new OpenTelemetryTrace("Request");

            public string Name;

            protected OpenTelemetryTrace(string name)
            {
                this.Name = name;
            }
        }
    }
}
