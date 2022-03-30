// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Collector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class HttpResponseSignature : IAllowListStatus
    {
        private readonly HttpStatusCode statusCode;
        private readonly Regex responseMessageRegex;
        private readonly Func<JObject, bool> matcher;
        private readonly Func<List<CollectionNode>> continuation;

        public HttpResponseSignature(HttpStatusCode statusCode, string responseMessageRegex)
        {
            this.statusCode = statusCode;
            this.responseMessageRegex = new Regex($"^{responseMessageRegex}$", RegexOptions.Compiled | RegexOptions.Multiline);
        }

        public HttpResponseSignature(HttpStatusCode statusCode, Func<JObject, bool> matcher, Func<List<CollectionNode>> continuation = null)
        {
            this.statusCode = statusCode;
            this.matcher = matcher;
            this.continuation = continuation;
        }

        public bool Matches(HttpStatusCode statusCode, JObject responseContent, string responseMessagePath = "$.message")
        {
            if (this.matcher != null)
            {
                return this.statusCode == statusCode && this.matcher.Invoke(responseContent);
            }

            string responseMessage = responseContent.SelectToken(responseMessagePath).Value<string>();
            return this.statusCode == statusCode && this.responseMessageRegex.IsMatch(responseMessage);
        }

        public List<CollectionNode> Continuation()
        {
            if (this.continuation == null)
            {
                return new List<CollectionNode>();
            }

            return this.continuation();
        }
    }
}
