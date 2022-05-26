// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Authentication;
using Microsoft.CloudMine.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public sealed class HttpClientWrapper : IHttpClient
    {
        public static readonly TimeSpan HttpTimeout = TimeSpan.FromMinutes(10);

        private readonly HttpClient httpClient;

        public HttpClientWrapper()
        {
            this.httpClient = new HttpClient()
            {
                Timeout = HttpTimeout,
            };
            this.httpClient.DefaultRequestHeaders.ConnectionClose = false;
        }

        public Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue)
        {
            return this.GetAsync(requestUrl, authentication, productInfoHeaderValue, eTag: string.Empty);
        }

        public Task<HttpResponseMessage> PostAsync(string requestUrl, string requestBody, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue)
        {
            return this.PostAsync(requestUrl, requestBody, authentication, productInfoHeaderValue, additionalHeaders: new Dictionary<string, string>());
        }

        public Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue, string eTag)
        {
            return this.GetAsync(requestUrl, authentication, productInfoHeaderValue, eTag, additionalHeaders: new Dictionary<string, string>());
        }

        public Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue, IDictionary<string, string> additionalHeaders)
        {
            return this.GetAsync(requestUrl, authentication, productInfoHeaderValue, eTag: string.Empty, additionalHeaders);
        }

        public Task<HttpResponseMessage> PostAsync(string requestUrl, string requestBody, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue, IDictionary<string, string> additionalHeaders)
        {
            return this.MakeRequestAsync(requestUrl, HttpMethod.Post, requestBody, authentication, productInfoHeaderValue, eTag: string.Empty, additionalHeaders);
        }

        private Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue, string eTag, IDictionary<string, string> additionalHeaders)
        {
            return this.MakeRequestAsync(requestUrl, HttpMethod.Get, requestBody: string.Empty, authentication, productInfoHeaderValue, eTag, additionalHeaders);
        }

        private async Task<HttpResponseMessage> MakeRequestAsync(string requestUrl, HttpMethod method, string requestBody, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue, string eTag, IDictionary<string, string> additionalHeaders)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = method,
                RequestUri = new Uri(requestUrl),
            };
            if (method == HttpMethod.Post)
            {
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Setup authentication
            string authenticationHeader = await authentication.GetAuthorizationHeaderAsync().ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue(authentication.Schema, authenticationHeader);

            // Setup additional headers
            foreach (KeyValuePair<string, string> additionalHeader in authentication.AdditionalWebRequestHeaders.Concat(additionalHeaders))
            {
                request.Headers.Add(additionalHeader.Key, additionalHeader.Value);
            }

            // Setup product info
            if (productInfoHeaderValue != null)
            {
                request.Headers.UserAgent.Add(productInfoHeaderValue);
            }

            // Setup e-tag
            if (!string.IsNullOrEmpty(eTag))
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(eTag));
            }

            HttpResponseMessage result = await this.httpClient.SendAsync(request).ConfigureAwait(false);
            return result;
        }
    }
}
