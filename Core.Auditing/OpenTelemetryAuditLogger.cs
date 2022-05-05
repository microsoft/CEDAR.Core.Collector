// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.CloudMine.Core.Telemetry;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Audit.Geneva;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.CloudMine.Core.Auditing
{

    public class TargetResource
    {
        public string Type { get; private set; }
        public string Name { get; private set; }
        public string Cluster { get; private set; }
        public string Region { get; private set; }

        public TargetResource(string type, string name, string cluster = null, string region = null)
        {
            this.Type = type;
            this.Name = name;
            this.Cluster = cluster;
            this.Region = region;
        }
    }

    public class CallerIdentity
    {
        public CallerIdentityType Type { get; private set; }
        public string Name { get; private set; }
        public string description { get; private set; }

        public CallerIdentity(CallerIdentityType type, string name, string description = null)
        {
            this.Type = type;
            this.Name = name;
            this.description = description;
        }
    }

    public class OpenTelemetryAuditLogger : IAuditLogger
    {
        private ILogger logger;
        private string webAppName;
        private string ipAddress;

        private const string TokenGenerationOperation = "TokenGeneration";
        private const string FetchCertificateOperation = "FetchCertificate";
        private const string DefaultWebAppName = "CloudMinePlatform";

        private static readonly AuditLoggerFactory AuditLoggerFactory = AuditLoggerFactory.Create(AuditOptions.DefaultForEtw);

        public OpenTelemetryAuditLogger()
        {
            this.logger = AuditLoggerFactory.CreateDataPlaneLogger(); // AsmAuditDP Jarvis table
            this.ipAddress = FetchIPAddress();
            this.webAppName = GetWebAppName();
        }

        private void LogAuditEvent(ITelemetryClient telemetryClient, OperationCategory operationCategory, OperationType operationType, string operationName, OperationResult operationResult, string operationResultDescription, List<CallerIdentity> callerIdentities, List<TargetResource> targetResources)
        {
            AuditRecord auditRecord = new AuditRecord();

            auditRecord.AddOperationCategory(operationCategory);
            auditRecord.OperationType = operationType;
            auditRecord.OperationName = operationName;
            auditRecord.OperationResult = operationResult;
            auditRecord.OperationResultDescription = operationResultDescription;

            foreach (CallerIdentity callerIdentity in callerIdentities)
            {
                auditRecord.AddCallerIdentity(callerIdentity.Type, callerIdentity.Name, callerIdentity.description);
            }

            foreach (TargetResource targetResource in targetResources)
            {
                auditRecord.AddTargetResource(targetResource.Type, targetResource.Name);
            }

            auditRecord.OperationAccessLevel = "GroupManager";
            auditRecord.CallerIpAddress = this.ipAddress;
            auditRecord.AddCallerAccessLevel("GroupOperator"); 
            auditRecord.CallerAgent = this.webAppName;

            try
            {
                this.logger.LogAudit(auditRecord);
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex, "GenevaAuditFailure");
            }
        }

        public void LogTokenGenerationAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, string operationResultDescription, List<TargetResource> targetResources, List<CallerIdentity> callerIdentities, string tokenType)
        {
            LogAuditEvent(telemetryClient, OperationCategory.Authorization, OperationType.Create, tokenType + TokenGenerationOperation, operationResult, operationResultDescription, callerIdentities, targetResources);
        }

        public void LogCertificateFetchAuditEvent(ITelemetryClient telemetryClient, OperationResult operationResult, string operationResultDescription, List<TargetResource> targetResources, List<CallerIdentity> callerIdentities)
        {
            LogAuditEvent(telemetryClient, OperationCategory.Authorization, OperationType.Read, FetchCertificateOperation, operationResult, operationResultDescription, callerIdentities, targetResources);
        }

        public void LogRequest(ITelemetryClient telemetryClient, OperationResult operationResult, string operationResultDescription, List<TargetResource> targetResources, List<CallerIdentity> callerIdentities, string operationName)
        {
            LogAuditEvent(telemetryClient, OperationCategory.CustomerFacing, OperationType.Read, $"Request {operationName}", operationResult, operationResultDescription, callerIdentities, targetResources);
        }

        private static string GetWebAppName()
        {
            string webAppName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
            if (string.IsNullOrEmpty(webAppName))
            {
                webAppName = DefaultWebAppName;
            }

            return webAppName;
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
    }
}
