// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Cloud.InstrumentationFramework;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public interface IAuditLogger
    {
        void Initialize(string tenantIdentity, string roleIdentity);

        void LogApplicationAuditEvent(ITelemetryClient telemetryClient, AuditMandatoryProperties auditMandatoryProperties, AuditOptionalProperties auditOptionalProperties = null);

        void LogTokenGenerationAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, AuditOptionalProperties auditOptionalProperties = null);

        void LogCertificateFetchAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, AuditOptionalProperties auditOptionalProperties = null);
    }
}
