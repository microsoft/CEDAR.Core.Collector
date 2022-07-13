// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class OpenTelemetryTelemetryClient : ITelemetryClient
    {
        private readonly string sessionId;

        public OpenTelemetryTelemetryClient(string sessionId)
        {
            this.sessionId = sessionId;
        }

        public void LogInformation(string message, IDictionary<string, string> additionalProperties = null)
        {
            using Activity trace = OpenTelemetryTracer.GetActivity(message).Start();
            trace.AddTag("Severity", "Information");

            Dictionary<string, string> properties = this.GetContextProperties();

            if (additionalProperties != null)
            {
                foreach (KeyValuePair<string, string> property in additionalProperties)
                {
                    properties[property.Key] = property.Value;
                }
            }

            foreach (string key in properties.Keys)
            {
                trace.AddTag(key, properties[key]);
            }
        }

        public void LogCritical(string message, IDictionary<string, string> additionalProperties = null)
        {
            using Activity trace = OpenTelemetryTracer.GetActivity(message).Start();
            trace.AddTag("Severity", "Critical");

            Dictionary<string, string> properties = this.GetContextProperties();
            if (additionalProperties != null)
            {
                foreach (KeyValuePair<string, string> property in additionalProperties)
                {
                    properties[property.Key] = property.Value;
                }
            }

            foreach (string key in properties.Keys)
            {
                trace.AddTag(key, properties[key]);
            }
        }

        public void TrackException(Exception exception, string message = null, IDictionary<string, string> additionalProperties = null)
        {
            using Activity trace = OpenTelemetryTracer.GetActivity("Exception").Start();
            trace.AddTag("ExceptionMessage", message);
            trace.AddTag("ExceptionType", exception.GetType().Name);

            if (exception.StackTrace != null)
            {
                trace.AddTag("ParsedStack", exception.StackTrace);
            }

            Dictionary<string, string> properties = this.GetContextProperties();
            if (additionalProperties != null)
            {
                foreach (KeyValuePair<string, string> property in additionalProperties)
                {
                    properties[property.Key] = property.Value;
                }
            }

            foreach (string key in properties.Keys)
            {
                trace.AddTag(key, properties[key]);
            }
        }

        public void LogWarning(string message, IDictionary<string, string> additionalProperties = null)
        {
            using Activity trace = OpenTelemetryTracer.GetActivity(message).Start();
            trace.AddTag("Severity", "Warning");

            Dictionary<string, string> properties = this.GetContextProperties();
            if (additionalProperties != null)
            {
                foreach (KeyValuePair<string, string> property in additionalProperties)
                {
                    properties[property.Key] = property.Value;
                }
            }

            foreach (string key in properties.Keys)
            {
                trace.AddTag(key, properties[key]);
            }
        }

        public void TrackEvent(string eventName, IDictionary<string, string> additionalProperties = null)
        {
            using Activity trace = OpenTelemetryTracer.GetActivity(eventName).Start();
            trace.AddTag("Severity", "Event");

            Dictionary<string, string> properties = this.GetContextProperties();
            if (additionalProperties != null)
            {
                foreach (KeyValuePair<string, string> property in additionalProperties)
                {
                    properties[property.Key] = property.Value;
                }
            }

            foreach (string key in properties.Keys)
            {
                trace.AddTag(key, properties[key]);
            }
        }

        public virtual void TrackRequest(string identity, string apiName, string requestUrl, string eTag, TimeSpan duration, HttpResponseMessage responseMessage, IDictionary<string, string> properties = null)
        {
            this.TrackRequest(identity, apiName, requestUrl, requestBody: string.Empty, eTag, duration, responseMessage, properties);
        }

        public void TrackRequest(string identity, string apiName, string requestUrl, string requestBody, string eTag, TimeSpan duration, HttpResponseMessage responseMessage, IDictionary<string, string> properties = null)
        {
            using Activity trace = OpenTelemetryTracer.GetActivity("Request").Start();

            IEnumerable<KeyValuePair<string, string>> allProperties = properties == null ? this.GetContextProperties() : this.GetContextProperties().Concat(properties);
            foreach (string key in properties.Keys)
            {
                trace.AddTag(key, properties[key]);
            }

            string identityToTrack = identity;
            bool guidIdentity = Guid.TryParse(identityToTrack, out Guid _);
            if (guidIdentity)
            {
                identityToTrack = identityToTrack.Substring(0, 4); // For security reasons, if the identity is a GUID, only capture the first 4 characters in the telemetry.
            }

            trace.AddTag("ApiName", apiName);
            trace.AddTag("Url", requestUrl);
            trace.AddTag("Duration", duration);
            trace.AddTag("ResultCode", responseMessage.StatusCode.ToString());
            trace.AddTag("RequestBody", requestBody);
            trace.AddTag("ETag", eTag);
            trace.AddTag("Identity", identityToTrack);
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
