// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Cloud.InstrumentationFramework;
using Microsoft.CloudMine.Core.Telemetry;

namespace Microsoft.CloudMine.Core.Auditing
{
    public interface IAuditLogger
    {
        void Initialize(string tenantIdentity, string roleIdentity);

        void LogApplicationAuditEvent(ITelemetryClient telemetryClient, AuditMandatoryProperties auditMandatoryProperties, AuditOptionalProperties auditOptionalProperties = null);

        void LogTokenGenerationAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, string tokenType);

        void LogCertificateFetchAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities);

        void LogRequest(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, string operationName);
    }
}
