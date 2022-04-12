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

        public void LogTokenGenerationAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, string tokenType)
        {
            LogAuditEvent(telemetryClient,  targetResources, callerIdentities, tokenType + TokenGenerationOperation, operationResult, AuditEventCategory.Authorization);
        }

        public void LogCertificateFetchAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities)
        {
            LogAuditEvent(telemetryClient, targetResources, callerIdentities, FetchCertificateOperation, operationResult, AuditEventCategory.Authorization);
        }

        public void LogRequest(ITelemetryClient telemetryClient, OperationResult operationResult, TargetResource[] targetResources, CallerIdentity[] callerIdentities, string operationName)
        {
            LogAuditEvent(telemetryClient, targetResources, callerIdentities, $"Request {operationName}", operationResult, AuditEventCategory.Other);
        }

        private void LogAuditEvent(ITelemetryClient telemetryClient, TargetResource[] targetResources, CallerIdentity[] callerIdentities, string operationName, OperationResult operationResult, AuditEventCategory auditEventCategory, AuditOptionalProperties auditOptionalProperties = null)
        {
            string webAppName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
            if (string.IsNullOrEmpty(webAppName))
            {
                telemetryClient?.LogWarning($"[{nameof(LogAuditEvent)}] Web app name isn't found from environment variable.");
                webAppName = DefaultWebAppName; // Set to default web app name.
            }
            AuditMandatoryProperties auditMandatoryProperties = new AuditMandatoryProperties()
            {
                ResultType = operationResult,
                OperationName = operationName
            };
            auditMandatoryProperties.AddCallerIdentities(callerIdentities);
            auditMandatoryProperties.AddTargetResources(targetResources);
<<<<<<< HEAD
            auditMandatoryProperties.ResultType = operationResult;

            //auditOptionalProperties.CallerDisplayName = 
=======
            auditMandatoryProperties.AddAuditCategory(auditEventCategory);
            auditOptionalProperties = new AuditOptionalProperties()
            {
                CallerDisplayName = webAppName
            };
>>>>>>> ca5fa683993f7afa3a5a61aa4e865fe2e0512311
            // And the most important part, calling the Audit functions: 
            this.LogApplicationAuditEvent(telemetryClient, auditMandatoryProperties, auditOptionalProperties);
        }

        public static string FetchIPAddress()
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
        public void LogRequest(ITelemetryClient telemetryClient, TargetResource[] targetResources, CallerIdentity[] callerIdentities, AuditMandatoryProperties auditMandatoryProperties, AuditOptionalProperties auditOptionalProperties = null)
        {
            string webAppName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
            if (string.IsNullOrEmpty(webAppName))
            {
                telemetryClient.LogWarning($"[{nameof(LogRequest)}] Web app name isn't found from environment variable.");
                webAppName = DefaultWebAppName; // Set to default web app name.
            }
            auditMandatoryProperties.AddCallerIdentity(new CallerIdentity(CallerIdentityType.ApplicationID, webAppName));
            //add audit category
            auditMandatoryProperties.AddAuditCategory(AuditEventCategory.Other);
            //add caller identities
            foreach (CallerIdentity callerIdentity in callerIdentities)
            {
                auditMandatoryProperties.AddCallerIdentity(callerIdentity);
            }
            //add target resources
            auditMandatoryProperties.AddTargetResources(targetResources);

            // And the most important part, calling the Audit function: 
            this.LogApplicationAuditEvent(telemetryClient, auditMandatoryProperties, auditOptionalProperties);
        }
    }
}
