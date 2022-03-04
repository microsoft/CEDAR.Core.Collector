// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Cloud.InstrumentationFramework;
using Microsoft.CloudMine.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.CloudMine.Core.Auditing
{
    public class IfxAuditLogger : IAuditLogger
    {
        private bool alreadyLoggedPerSession = false;
        private const string TokenGenerationOperation = "TokenGeneration";
        private const string FetchCertificateOperation = "FetchCertificate";
        private const string DefaultWebAppName = "CloudMinePlatform";

        /// <summary>
        /// Initializes audit logging.
        /// </summary>
        public void Initialize(string tenantIdentity, string roleIdentity)
        {
            string roleInstanceIdentity = FetchIPAddress();
            IfxInitializer.IfxInitialize(tenantIdentity, roleIdentity, roleInstanceIdentity);
        }

        /// <summary>
        /// Log audit event
        /// </summary>
        /// <param name="telemetryClient">This can be null when invoked from ICM service</param>
        /// <param name="auditMandatoryProperties">Mandatory properties to log</param>
        /// <param name="auditOptionalProperties">This is optional app specific properties</param>
        public void LogApplicationAuditEvent(ITelemetryClient telemetryClient, AuditMandatoryProperties auditMandatoryProperties, AuditOptionalProperties auditOptionalProperties = null)
        {
            IfxResult ifxResult = IfxResult.Failure(IfxResultCode.UnknownError, "Unknown Error");
            bool appLogResult = IfxAudit.LogApplicationAudit(auditMandatoryProperties, auditOptionalProperties, ifxResult);
            bool managementLogResult = IfxAudit.LogManagementAudit(auditMandatoryProperties, auditOptionalProperties, ifxResult);

            if ((!appLogResult || !managementLogResult) && !alreadyLoggedPerSession)
            {
                /* In local box environment, audit logging mightn't be saved.
                In order to locally debug your code, please download and execute RunBefore_IFxAuditWebAppsLocalTesting.cmd, 
                this sets a couple of environment variables that are present in Web Apps and IFxAudit needs.After running that please make sure you restart
                Visual Studio so that the changes are picked up.(See https://genevamondocs.azurewebsites.net/collect/references/ifxref/ifxAuditWebApps.html)
                */
                Dictionary<string, string> properties = new Dictionary<string, string>()
                {
                    { "IfxResultMessage", ifxResult.Message },
                    { "IfxResultCode", ifxResult.Code.ToString() },
                };
                telemetryClient.TrackEvent("IfxApplicationAuditFailure", properties);
                alreadyLoggedPerSession = true;
            }
            else
            {
                // If there is ever success for sending the logs, reset "alreadyLoggedPerSession" so that we capture transient failures more accurately.
                alreadyLoggedPerSession = false;
            }
        }

        public void LogTokenGenerationAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, string tokenType, AuditOptionalProperties auditOptionalProperties = null)
        {
            LogAuthorizationAuditEvent(telemetryClient, operationResult, tokenType + TokenGenerationOperation, targetResources, callerIdentities, auditOptionalProperties);
        }

        public void LogCertificateFetchAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, AuditOptionalProperties auditOptionalProperties = null)
        {
            LogAuthorizationAuditEvent(telemetryClient, operationResult, FetchCertificateOperation, targetResources, callerIdentities, auditOptionalProperties);
        }

        private void LogAuthorizationAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, string operationName, TargetResource[] targetResources, CallerIdentity[] callerIdentities, AuditOptionalProperties auditOptionalProperties)
        {
            string webAppName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
            if (string.IsNullOrEmpty(webAppName))
            {
                telemetryClient?.LogWarning($"[{nameof(LogAuthorizationAuditEvent)}] Web app name isn't found from environment variable.");
                webAppName = DefaultWebAppName; // Set to default web app name.
            }

            var auditMandatoryProperties = new AuditMandatoryProperties
            {
                OperationName = operationName
            };

            auditMandatoryProperties.AddAuditCategory(AuditEventCategory.Authorization);
            auditMandatoryProperties.AddCallerIdentity(new CallerIdentity(CallerIdentityType.ApplicationID, webAppName));
            foreach (CallerIdentity callerIdentity in callerIdentities)
            {
                auditMandatoryProperties.AddCallerIdentity(callerIdentity);
            }
            auditMandatoryProperties.AddTargetResources(targetResources);
            auditMandatoryProperties.ResultType = operationResult;

            // And the most important part, calling the Audit functions: 
            this.LogApplicationAuditEvent(telemetryClient, auditMandatoryProperties, auditOptionalProperties);
        }

        private static string FetchIPAddress()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(Environment.MachineName);
            string ipAddress = null;

            foreach (var addr in addresses)
            {
                if (addr.ToString() == "127.0.0.1")
                {
                    continue;
                }
                else if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = addr.ToString();
                    break;
                }
            }

            return ipAddress;
        }
    }
}
