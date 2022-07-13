// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Telemetry
{
    public class NoopTelemetryClient : ITelemetryClient
    {
        public void LogCritical(string message, IDictionary<string, string> properties = null)
        {
            // Assume success.
        }

        public void LogInformation(string message, IDictionary<string, string> properties = null)
        {
            // Assume success.
        }

        public void LogWarning(string message, IDictionary<string, string> properties = null)
        {
            // Assume success.
        }

        public void TrackEvent(string eventName, IDictionary<string, string> properties = null)
        {
            // Assume success.
        }

        public void TrackException(Exception exception, string message = null, IDictionary<string, string> properties = null)
        {
            // Assume success.
        }

        public void TrackRequest(string identity, string apiName, string requestUrl, string eTag, TimeSpan duration, HttpResponseMessage responseMessage, IDictionary<string, string> properties = null)
        {
            // Assume success.
        }

        public void TrackRequest(string identity, string apiName, string requestUrl, string requestBody, string eTag, TimeSpan duration, HttpResponseMessage responseMessage, IDictionary<string, string> properties = null)
        {
            // Assume success.
        }
    }
}
