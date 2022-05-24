// Copyright (c) Microsoft Corporation. All rights reserved.

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

        public static Activity GetActivity(string name)
        {
            return ActivitySource.CreateActivity(name, ActivityKind.Internal).AddDefaultTags();

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

                    tracerProvider = Sdk.CreateTracerProviderBuilder().SetSampler(new AlwaysOnSampler()).AddSource(SUBSCRIPTION_KEY).AddGenevaTraceExporter(options =>
                    {
                        options.ConnectionString = "EtwSession=OpenTelemetry";
                    }).Build();

                    break;
                default:
                    tracerProvider = Sdk.CreateTracerProviderBuilder().SetSampler(new AlwaysOnSampler()).AddSource(SUBSCRIPTION_KEY).AddConsoleExporter().Build();
                    break;
            }

            return tracerProvider;
        }
    }
}
