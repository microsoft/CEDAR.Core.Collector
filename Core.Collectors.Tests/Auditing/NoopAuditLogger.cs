using Microsoft.Cloud.InstrumentationFramework;
using Microsoft.CloudMine.Core.Auditing;
using Microsoft.CloudMine.Core.Telemetry;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Auditing
{
    public class NoopAuditLogger : IAuditLogger
    {
        public void Initialize(string tenantIdentity, string roleIdentity)
        {
            // Assume success.
        }
        public void LogApplicationAuditEvent(ITelemetryClient telemetryClient, AuditMandatoryProperties auditMandatoryProperties, AuditOptionalProperties auditOptionalProperties = null)
        {
            // Assume success.
        }

        public void LogTokenGenerationAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, string tokenType, AuditOptionalProperties auditOptionalProperties = null)
        {
            // Assume success.
        }

        public void LogCertificateFetchAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, AuditOptionalProperties auditOptionalProperties = null)
        {
            // Assume success.
        }

        public void LogRequest(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, AuditMandatoryProperties auditMandatoryProperties, AuditOptionalProperties auditOptionalProperties = null)
        {
            // Assume success.
        }
    }
}
