// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.CloudMine.Core.Telemetry
{
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
