// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class ApplicationInsightsTelemetryClient : ITelemetryClient
    {
        private readonly TelemetryClient telemetryClient;
        private readonly string sessionId;

        public ApplicationInsightsTelemetryClient(TelemetryClient telemetryClient, string sessionId, ILogger logger)
            :this(telemetryClient, sessionId)
        {
            // TODO: Luke G - Remove this constructor, left in temporarilly to make change for non-breaking
        }

        public ApplicationInsightsTelemetryClient(TelemetryClient telemetryClient, string sessionId)
        {
            this.telemetryClient = telemetryClient;
            this.sessionId = sessionId;
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
                    properties[property.Key] = property.Value;
                }
            }

            this.telemetryClient.TrackTrace(message, SeverityLevel.Warning, properties);
        }

        public void TrackEvent(string eventName, IDictionary<string, string> additionalProperties = null)
        {
            Dictionary<string, string> properties = this.GetContextProperties();
            if (additionalProperties != null)
            {
                foreach (KeyValuePair<string, string> property in additionalProperties)
                {
                    properties[property.Key] = property.Value;
                }
            }

            this.telemetryClient.TrackEvent(eventName, properties);
        }

        public virtual void TrackRequest(string identity, string apiName, string requestUrl, string eTag, TimeSpan duration, HttpResponseMessage responseMessage)
        {
            this.TrackRequest(identity, apiName, requestUrl, requestBody: string.Empty, eTag, duration, responseMessage, properties: null);
        }

        public virtual void TrackRequest(string identity, string apiName, string requestUrl, string eTag, TimeSpan duration, HttpResponseMessage responseMessage, IDictionary<string, string> properties = null)
        {
            this.TrackRequest(identity, apiName, requestUrl, requestBody: string.Empty, eTag, duration, responseMessage, properties);
        }

        public void TrackRequest(string identity, string apiName, string requestUrl, string requestBody, string eTag, TimeSpan duration, HttpResponseMessage responseMessage, IDictionary<string, string> properties = null)
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
            IEnumerable<KeyValuePair<string, string>> allProperties = properties == null ? this.GetContextProperties() : this.GetContextProperties().Concat(properties);
            foreach (KeyValuePair<string, string> property in allProperties)
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
