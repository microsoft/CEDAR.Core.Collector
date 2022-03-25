// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class HttpResponseSignature
    {
        private readonly HttpStatusCode statusCode;
        private readonly Regex responseMessageRegex;
        private readonly Func<JObject, bool> matcher;
        private readonly Func<Task> handler;

        public HttpResponseSignature(HttpStatusCode statusCode, string responseMessageRegex, Func<Task> handler = null)
        {
            this.statusCode = statusCode;
            this.responseMessageRegex = new Regex($"^{responseMessageRegex}$", RegexOptions.Compiled | RegexOptions.Multiline);
            this.handler = handler;
        }

        public HttpResponseSignature(HttpStatusCode statusCode, Func<JObject, bool> matcher, Func<Task> handler = null)
        {
            this.statusCode = statusCode;
            this.matcher = matcher;
            this.handler = handler;
        }

        public async Task<bool> Matches(HttpResponseMessage response, string responseMessagePath = "$.message")
        {
            try
            {
                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                JObject responseContentObject = JObject.Parse(responseContent);
                return this.Matches(response.StatusCode, responseContentObject, responseMessagePath);
            }
            catch (JsonReaderException)
            {
                return false;
            }
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

        public Task Handle()
        {
            if (null != this.handler)
            {
                return this.handler.Invoke();
            }
            return Task.CompletedTask;
        }
    }
}
