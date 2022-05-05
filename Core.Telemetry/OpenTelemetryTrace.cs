// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class OpenTelemetryTrace
    {
        public static readonly OpenTelemetryTrace FunctionInvocation = new OpenTelemetryTrace("FunctionInvocation");
        public static readonly OpenTelemetryTrace ProccessCollectionNode = new OpenTelemetryTrace("ProccessCollectionNode");
        public static readonly OpenTelemetryTrace Heartbeat = new OpenTelemetryTrace("Heartbeat");
        public static readonly OpenTelemetryTrace Request = new OpenTelemetryTrace("Request");

        public string Name { get; private set; }

        protected OpenTelemetryTrace(string name)
        {
            this.Name = name;
        }
    }
}
