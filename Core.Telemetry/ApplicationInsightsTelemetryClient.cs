// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.CloudMine.Core.Collectors.Telemetry
{
    public class ApplicationInsightsTelemetryClient : ITelemetryClient
    {
        private readonly TelemetryClient telemetryClient;
        private readonly string sessionId;
        private readonly ILogger logger;

        public ApplicationInsightsTelemetryClient(TelemetryClient telemetryClient, string sessionId, ILogger logger = null)
        {
            this.telemetryClient = telemetryClient;
            this.sessionId = sessionId;
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
                    if (properties.ContainsKey(property.Key))
                    {
                        properties[property.Key] = property.Value;
                    }
                    else
                    {
                        properties.Add(property.Key, property.Value);
                    }
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
                    if (properties.ContainsKey(property.Key))
                    {
                        properties[property.Key] = property.Value;
                    }
                    else
                    {
                        properties.Add(property.Key, property.Value);
                    }
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

            string identityToTrack = identity;
            bool guidIdentity = Guid.TryParse(identityToTrack, out Guid _);
            if (guidIdentity)
            {
                identityToTrack = identityToTrack.Substring(0, 4); // For security reasons, if the identity is a GUID, only capture the first 4 characters in the telemetry.
            }

            dependencyTelemetry.Properties.Add("Identity", identityToTrack);
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
                { "SessionId", this.sessionId },
            };
        }
    }
}
