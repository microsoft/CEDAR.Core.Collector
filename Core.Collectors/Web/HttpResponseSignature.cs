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
        public HttpStatusCode statusCode;
        public HttpStatusCode StatusCode 
        {
            get
            {
                return this.statusCode;
            }
            set
            {
                this.statusCode = value;
            }
        }
        public Regex ResponseMessageRegex { get; }
        public string ResponseMessagePath { get; }
        public Func<JObject, bool> Matcher { get; }

        public HttpResponseSignature(HttpStatusCode statusCode, string responseMessageRegex, string responseMessagePath = "$.message")
        {
            this.StatusCode = statusCode;
            this.ResponseMessageRegex = new Regex($"^{responseMessageRegex}$", RegexOptions.Compiled | RegexOptions.Multiline);
            this.ResponseMessagePath = responseMessagePath;
        }

        public HttpResponseSignature(HttpStatusCode statusCode, Func<JObject, bool> matcher)
        {
            this.StatusCode = statusCode;
            this.Matcher = matcher;
        }

        public bool Matches(HttpStatusCode statusCode, string responseMessage)
        {
            return this.StatusCode == statusCode && this.ResponseMessageRegex.IsMatch(responseMessage);
        }

        public bool Matches(HttpStatusCode statusCode, JObject responseContent)
        {
            if (this.Matcher != null)
            {
                return this.StatusCode == statusCode && this.Matcher.Invoke(responseContent);
            }

            string responseMessage = responseContent.SelectToken("$.message").Value<string>();
            return this.Matches(this.StatusCode, responseMessage);
        }
    }
}
