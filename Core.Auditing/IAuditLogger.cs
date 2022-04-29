// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.CloudMine.Core.Telemetry;
using OpenTelemetry.Audit.Geneva;
using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Auditing
{
    public interface IAuditLogger
    {
        void Initialize();

        void LogTokenGenerationAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, List<TargetResource> targetResources, List<CallerIdentity> callerIdentities, string tokenType);

        void LogCertificateFetchAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, List<TargetResource> targetResources, List<CallerIdentity> callerIdentities);

        void LogRequest(ITelemetryClient telemetryClient, OperationResult operationResult, List<TargetResource> targetResources, List<CallerIdentity> callerIdentities, string operationName);
    }
}
