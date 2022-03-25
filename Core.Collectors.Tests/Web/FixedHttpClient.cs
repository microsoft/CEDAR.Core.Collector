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
        private readonly Dictionary<string, HttpResponseMessage> requestToResponseMap;

        public FixedHttpClient()
        {
            this.requestToResponseMap = new Dictionary<string, HttpResponseMessage>();
        }

        public void Reset()
        {
            this.requestToResponseMap.Clear();
        }

        public void AddResponse(string requestUrl, HttpStatusCode responseStatusCode, string responseMessage)
        {
            HttpResponseMessage response = new HttpResponseMessage()
            {
                StatusCode = responseStatusCode,
                Content = new StringContent(responseMessage),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl)
            };
            this.requestToResponseMap.Add(requestUrl, response);
        }

        public void AddResponse(string requestUrl, string body, HttpStatusCode responseStatusCode, string responseMessage)
        {
            HttpResponseMessage response = new HttpResponseMessage()
            {
                StatusCode = responseStatusCode,
                Content = new StringContent(responseMessage),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl)
            };
            this.requestToResponseMap.Add(requestUrl + body, response);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication)
        {
            if (this.requestToResponseMap.TryGetValue(requestUrl, out HttpResponseMessage result))
            {
                return Task.FromResult(result);
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

        public Task<HttpResponseMessage> PostAsync(string requestUrl, IAuthentication authentication, string requestBody)
        {
            if (this.requestToResponseMap.TryGetValue(requestUrl + requestBody, out HttpResponseMessage result))
            {
                return Task.FromResult(result);
            }

            if (this.requestToResponseMap.TryGetValue(requestUrl, out result))
            {
                return Task.FromResult(result);
            }

            throw new Exception($"FixedHttpClient: Unknown request '{requestUrl}'.");
        }
    }
}
