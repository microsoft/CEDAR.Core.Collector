// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Authentication;
using Microsoft.CloudMine.Core.Collectors.Web;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Web
{
    public class FixedHttpClient : IHttpClient
    {
        public int RequestCount { get; private set; }

        private readonly Dictionary<string, HttpResponseMessage> responseMap;
        private readonly Dictionary<string, Func<Tuple<HttpStatusCode, string>>> requestToResponseGeneratorMap;

        public FixedHttpClient()
        {
            this.responseMap = new Dictionary<string, HttpResponseMessage>();
            this.requestToResponseGeneratorMap = new Dictionary<string, Func<Tuple<HttpStatusCode, string>>>();
            this.RequestCount = 0;
        }

        public void Reset()
        {
            this.responseMap.Clear();
            this.requestToResponseGeneratorMap.Clear();
            this.RequestCount = 0;
        }

        public void AddResponseGenerator(string requestUrl, Func<Tuple<HttpStatusCode, string>> resposneGenerator)
        {
            this.requestToResponseGeneratorMap.Add(requestUrl, resposneGenerator);
        }

        public void AddResponseGenerator(string requestUrl, string responseBody, Func<Tuple<HttpStatusCode, string>> resposneGenerator)
        {
            this.requestToResponseGeneratorMap.Add(requestUrl + responseBody, resposneGenerator);
        }

        public void AddResponse(string requestUrl, HttpStatusCode responseStatusCode, string responseMessage, Dictionary<string, List<string>> responseHeaders = null)
        {
            this.AddResponse(requestUrl, string.Empty, responseStatusCode, responseMessage, responseHeaders);
        }

        public void AddResponse(string requestUrl, string requestBody, HttpStatusCode responseStatusCode, string responseMessage, Dictionary<string, List<string>> responseHeaders = null)
        {
            requestUrl += requestBody;
            HttpResponseMessage response = new HttpResponseMessage()
            {
                StatusCode = responseStatusCode,
                Content = new StringContent(responseMessage)
            };

            if (responseHeaders != null)
            {
                foreach (KeyValuePair<string, List<string>> header in responseHeaders)
                {
                    response.Headers.Add(header.Key, header.Value);
                }
            }

            this.responseMap.Add(requestUrl, response);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication)
        {
            this.RequestCount++;

            if (this.responseMap.TryGetValue(requestUrl, out HttpResponseMessage response))
            {
                return CopyHttpResponseMessage(response);
            }

            if (this.requestToResponseGeneratorMap.TryGetValue(requestUrl, out Func<Tuple<HttpStatusCode, string>> responseGenerator))
            {
                (HttpStatusCode responseCode, string responseContent) = responseGenerator();
                return Task.FromResult(new HttpResponseMessage()
                {
                    StatusCode = responseCode,
                    Content = new StringContent(responseContent)
                });
            }

            throw new Exception($"FixedHttpClient: Unknown request '{requestUrl}'.");
        }

        public Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue)
        {
            return this.GetAsync(requestUrl, authentication);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue, string eTag)
        {
            return this.GetAsync(requestUrl, authentication);
        }

        public Task<HttpResponseMessage> PostAsync(string requestUrl, string requestBody, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue)
        {
            return this.PostAsync(requestUrl, authentication, requestBody);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue, IDictionary<string, string> additionalHeaders)
        {
            return this.GetAsync(requestUrl, authentication);
        }

        public Task<HttpResponseMessage> PostAsync(string requestUrl, string requestBody, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue, IDictionary<string, string> additionalHeaders)
        {
            return this.PostAsync(requestUrl, authentication, requestBody);
        }

        public Task<HttpResponseMessage> PostAsync(string requestUrl, IAuthentication authentication, string requestBody)
        {
            this.RequestCount++;

            if (this.responseMap.TryGetValue(requestUrl + requestBody, out HttpResponseMessage cachedResponse))
            {
                return CopyHttpResponseMessage(cachedResponse);
            }

            if (this.responseMap.TryGetValue(requestUrl, out cachedResponse))
            {
                return CopyHttpResponseMessage(cachedResponse);
            }

            if (this.requestToResponseGeneratorMap.TryGetValue(requestUrl + requestBody, out Func<Tuple<HttpStatusCode, string>> responseGenerator))
            {
                (HttpStatusCode responseCode, string responseContent) = responseGenerator();
                return Task.FromResult(new HttpResponseMessage()
                {
                    StatusCode = responseCode,
                    Content = new StringContent(responseContent)
                });
            }

            throw new Exception($"FixedHttpClient: Unknown request '{requestUrl}'.");
        }

        private async Task<HttpResponseMessage> CopyHttpResponseMessage(HttpResponseMessage response)
        {
            HttpResponseMessage responseCopy = new HttpResponseMessage()
            {
                // Copying response message using ReadAsStringAsync ensures that multiple reads of Content are safe.
                Content = new StringContent(await response.Content.ReadAsStringAsync().ConfigureAwait(false)),
                StatusCode = response.StatusCode
            };

            foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
            {
                responseCopy.Headers.Add(header.Key, header.Value);
            }

            return responseCopy;
        }
    }
}
