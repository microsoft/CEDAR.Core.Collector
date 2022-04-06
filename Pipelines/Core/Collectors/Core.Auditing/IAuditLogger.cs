// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Cloud.InstrumentationFramework;
using Microsoft.CloudMine.Core.Telemetry;

namespace Microsoft.CloudMine.Core.Auditing
{
    public interface IAuditLogger
    {
        void Initialize(string tenantIdentity, string roleIdentity);

        void LogApplicationAuditEvent(ITelemetryClient telemetryClient, AuditMandatoryProperties auditMandatoryProperties, AuditOptionalProperties auditOptionalProperties = null);

        void LogTokenGenerationAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, string tokenType, AuditOptionalProperties auditOptionalProperties = null);

        void LogCertificateFetchAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, AuditOptionalProperties auditOptionalProperties = null);

        void LogRequest(ITelemetryClient telemetryClient, TargetResource[] targetResources, CallerIdentity[] callerIdentities, AuditMandatoryProperties auditMandatoryProperties, AuditOptionalProperties auditOptionalProperties = null);
    }
}
