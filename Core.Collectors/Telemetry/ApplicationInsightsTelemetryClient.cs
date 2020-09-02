// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.CloudMine.Core.Collectors.Context;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.CloudMine.Core.Collectors.Telemetry
{
    public class ApplicationInsightsTelemetryClient : ITelemetryClient
    {
        private readonly TelemetryClient telemetryClient;
        private readonly FunctionContext context;
        private readonly ILogger logger;

        public ApplicationInsightsTelemetryClient(TelemetryClient telemetryClient, FunctionContext context, ILogger logger = null)
        {
            this.telemetryClient = telemetryClient;
            this.context = context;
            this.logger = logger;
        }

        public void LogInformation(string message, IDictionary<string, string> additionalProperties = null)
        {
            Dictionary<string, string> properties = this.GetContextProperties();
            if (additionalProperties != null)
            {
                foreach (KeyValuePair<string, string> property in additionalProperties)
                {
                    properties.Add(property.Key, property.Value);
                }
            }
            properties.Add("Message", message);

            this.telemetryClient.TrackTrace(message, SeverityLevel.Information, properties);
            this.logger?.LogInformation(message);
        }

        public void LogCritical(string message, IDictionary<string, string> additionalProperties = null)
        {
            Dictionary<string, string> properties = this.GetContextProperties();
            if (additionalProperties != null)
            {
                foreach (KeyValuePair<string, string> property in additionalProperties)
                {
                    properties.Add(property.Key, property.Value);
                }
            }
            properties.Add("Message", message);

            this.telemetryClient.TrackTrace(message, SeverityLevel.Critical, properties);
            this.logger?.LogCritical(message);
        }

        public void TrackException(Exception exception, string message = null, IDictionary<string, string> additionalProperties = null)
        {
            Dictionary<string, string> properties = this.GetContextProperties();
            if (additionalProperties != null)
            {
                foreach (KeyValuePair<string, string> property in additionalProperties)
                {
                    properties.Add(property.Key, property.Value);
                }
            }
            properties.Add("Message", message);

            this.telemetryClient.TrackException(exception, properties);
        }

        public void LogWarning(string message, IDictionary<string, string> additionalProperties = null)
        {
            Dictionary<string, string> properties = this.GetContextProperties();
            if (additionalProperties != null)
            {
                foreach (KeyValuePair<string, string> property in additionalProperties)
                {
                    properties.Add(property.Key, property.Value);
                }
            }

            this.telemetryClient.TrackTrace(message, SeverityLevel.Warning, properties);
            this.logger?.LogWarning(message);
        }

        public void TrackEvent(string eventName, IDictionary<string, string> additionalProperties = null)
        {
            Dictionary<string, string> properties = this.GetContextProperties();
            if (additionalProperties != null)
            {
                foreach (KeyValuePair<string, string> property in additionalProperties)
                {
                    properties.Add(property.Key, property.Value);
                }
            }

            this.telemetryClient.TrackEvent(eventName, properties);
        }

        public virtual void TrackRequest(string identity, string apiName, string requestUrl, string eTag, TimeSpan duration, HttpResponseMessage responseMessage)
        {
            this.TrackRequest(identity, apiName, requestUrl, requestBody: string.Empty, eTag, duration, responseMessage);
        }

        public void TrackRequest(string identity, string apiName, string requestUrl, string requestBody, string eTag, TimeSpan duration, HttpResponseMessage responseMessage)
        {
            DependencyTelemetry dependencyTelemetry = new DependencyTelemetry()
            {
                Name = apiName,
                Target = requestUrl,
                Duration = duration,
                ResultCode = responseMessage.StatusCode.ToString(),
                Type = "External",
            };

            dependencyTelemetry.Properties.Add("RequestBody", requestBody);
            dependencyTelemetry.Properties.Add("ETag", eTag);
            dependencyTelemetry.Properties.Add("Identity", identity);
            foreach (KeyValuePair<string, string> property in this.GetContextProperties())
            {
                dependencyTelemetry.Properties.Add(property.Key, property.Value);
            }
            this.telemetryClient.TrackDependency(dependencyTelemetry);
        }

        private Dictionary<string, string> GetContextProperties()
        {
            return new Dictionary<string, string>()
            {
                { "SessionId", this.context.SessionId },
            };
        }
    }
}
