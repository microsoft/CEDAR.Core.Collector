// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class HttpResponseSignature
    {
        public HttpStatusCode StatusCode { get; }
        public Regex ResponseMessageRegex { get; }
        public Func<JObject, bool> Matcher { get; }

        public HttpResponseSignature(HttpStatusCode statusCode, string responseMessageRegex)
        {
            this.StatusCode = statusCode;
            this.ResponseMessageRegex = new Regex($"^{responseMessageRegex}$", RegexOptions.Compiled | RegexOptions.Multiline);
        }

        public HttpResponseSignature(HttpStatusCode statusCode, Func<JObject, bool> matcher)
        {
            this.StatusCode = statusCode;
            this.Matcher = matcher;
        }

        public bool Matches(HttpStatusCode statusCode, JObject responseContent, string responseMessagePath = "$.message")
        {
            if (this.Matcher != null)
            {
                return this.StatusCode == statusCode && this.Matcher.Invoke(responseContent);
            }

            string responseMessage = responseContent.SelectToken(responseMessagePath).Value<string>();
            return this.StatusCode == statusCode && this.ResponseMessageRegex.IsMatch(responseMessage);
        }
    }
}
