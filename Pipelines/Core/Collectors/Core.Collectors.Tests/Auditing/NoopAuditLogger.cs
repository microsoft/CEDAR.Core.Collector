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

<<<<<<< HEAD
        public void LogTokenGenerationAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, string tokenType, AuditOptionalProperties auditOptionalProperties = null)
=======
        public void LogTokenGenerationAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, string tokenType)
>>>>>>> ca5fa683993f7afa3a5a61aa4e865fe2e0512311
        {
            // Assume success.
        }

<<<<<<< HEAD
        public void LogCertificateFetchAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, AuditOptionalProperties auditOptionalProperties = null)
=======
        public void LogCertificateFetchAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities)
>>>>>>> ca5fa683993f7afa3a5a61aa4e865fe2e0512311
        {
            // Assume success.
        }

<<<<<<< HEAD
        public void LogRequest(ITelemetryClient telemetryClient, TargetResource[] targetResources, CallerIdentity[] callerIdentities, AuditMandatoryProperties auditMandatoryProperties, AuditOptionalProperties auditOptionalProperties = null)
=======
        public void LogRequest(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, string operationName)
>>>>>>> ca5fa683993f7afa3a5a61aa4e865fe2e0512311
        {
            // Assume success.
        }
    }
}
