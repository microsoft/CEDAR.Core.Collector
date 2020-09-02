// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.CloudMine.Core.Collectors.Telemetry
{
    public interface ITelemetryClient
    {
        void TrackEvent(string eventName, IDictionary<string, string> properties = null);
        void LogWarning(string message, IDictionary<string, string> properties = null);
        void LogCritical(string message, IDictionary<string, string> properties = null);
        void LogInformation(string message, IDictionary<string, string> properties = null);
        void TrackException(Exception exception, string message = null, IDictionary<string, string> properties = null);
        void TrackRequest(string identity, string apiName, string requestUrl, string eTag, TimeSpan duration, HttpResponseMessage responseMessage);
        void TrackRequest(string identity, string apiName, string requestUrl, string requestBody, string eTag, TimeSpan duration, HttpResponseMessage responseMessage);
    }
}
