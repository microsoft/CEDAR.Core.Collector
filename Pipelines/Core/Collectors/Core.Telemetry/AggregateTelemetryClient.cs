using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public class AggregateTelemetryClient : ITelemetryClient
    {

        private readonly IEnumerable<ITelemetryClient> telemetryClients;

        public AggregateTelemetryClient(IEnumerable<ITelemetryClient> telemetryClients)
        {
            this.telemetryClients = telemetryClients;
        }

        public void LogCritical(string message, IDictionary<string, string> properties = null)
        {
            foreach (ITelemetryClient telemetryClient in this.telemetryClients)
            {
                telemetryClient.LogCritical(message, properties);
            }
        }

        public void LogInformation(string message, IDictionary<string, string> properties = null)
        {
            foreach (ITelemetryClient telemetryClient in this.telemetryClients)
            {
                telemetryClient.LogInformation(message, properties);
            }
        }

        public void LogWarning(string message, IDictionary<string, string> properties = null)
        {
            foreach (ITelemetryClient telemetryClient in this.telemetryClients)
            {
                telemetryClient.LogWarning(message, properties);
            }
        }

        public void TrackEvent(string eventName, IDictionary<string, string> properties = null)
        {
            foreach (ITelemetryClient telemetryClient in this.telemetryClients)
            {
                telemetryClient.TrackEvent(eventName, properties);
            }
        }

        public void TrackException(Exception exception, string message = null, IDictionary<string, string> properties = null)
        {
            foreach (ITelemetryClient telemetryClient in this.telemetryClients)
            {
                telemetryClient.TrackException(exception, message, properties);
            }
        }

        public void TrackRequest(string identity, string apiName, string requestUrl, string eTag, TimeSpan duration, HttpResponseMessage responseMessage)
        {
            foreach (ITelemetryClient telemetryClient in this.telemetryClients)
            {
                telemetryClient.TrackRequest(identity, apiName, requestUrl, eTag, duration, responseMessage);
            }
        }

        public void TrackRequest(string identity, string apiName, string requestUrl, string requestBody, string eTag, TimeSpan duration, HttpResponseMessage responseMessage)
        {
            foreach (ITelemetryClient telemetryClient in this.telemetryClients)
            {
                telemetryClient.TrackRequest(identity, apiName, requestUrl, requestBody, eTag, duration, responseMessage);
            }
        }
    }
}
