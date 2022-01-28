// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.CloudMine.Core.Collectors.Utility
{
    public static class ErrorMessages
    {
        public const string AppTokenRequestFailed = "appToken Generation - request failed with error";
        public const string AppTokenEmptyResponseBody = "appToken Generation - empty response body returned";
        public const string AppTokenErrorResponseBody = "appToken Generation - error with the response body";
        public const string AppTokenNoAccessTokenField = "appToken Generation - no access_token field returned";
        public const string AppTokenEmptyAccessTokenField = "appToken Generation - empty access_token field returned";

        public const string NotificationEventTypeNotSupported = "This event type is not supported";
        public const string EmptyEventPayload = "GetPullRequestDetails returned null or invalid pull request details.";
        public const string InvalidExtensionName = "Invalid extension name";
        public const string InvalidEnvName = "Invalid environment referrenced";
        public const string InvalidRequestedScopes = "Requested scopes are not subset of the default CloudMine app scope";
        public const string InvalidExternalEvent = "Invalid external event";
        public const string RequestedScopesNotApplicable = "No applicable scopes were found for this event";
    }
}
