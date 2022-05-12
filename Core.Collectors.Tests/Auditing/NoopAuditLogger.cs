using Microsoft.CloudMine.Core.Auditing;
using Microsoft.CloudMine.Core.Telemetry;
using OpenTelemetry.Audit.Geneva;
using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Auditing
{
    public class NoopAuditLogger : IAuditLogger
    {
        public void Initialize()
        {
            // Assume success.
        }

        public void LogTokenGenerationAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, string operationResultDescription, List<TargetResource> targetResources, List<CallerIdentity> callerIdentities, string tokenType)
        {
            // Assume success.
        }

        public void LogCertificateFetchAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, string operationResultDescription, List<TargetResource> targetResources, List<CallerIdentity> callerIdentities)
        {
            // Assume success.
        }

        public void LogRequest(ITelemetryClient telemetryClient, OperationResult operationResult, string operationResultDescription, List<TargetResource> targetResources, List<CallerIdentity> callerIdentities, string operationName)
        {
            // Assume success.
        }
    }
}
