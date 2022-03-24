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

        public HttpResponseSignature(HttpStatusCode statusCode, string responseMessageRegex)
        {
            this.statusCode = statusCode;
            this.responseMessageRegex = new Regex($"^{responseMessageRegex}$", RegexOptions.Compiled | RegexOptions.Multiline);
        }

        public HttpResponseSignature(HttpStatusCode statusCode, Func<JObject, bool> matcher)
        {
            this.statusCode = statusCode;
            this.matcher = matcher;
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
    }
}
