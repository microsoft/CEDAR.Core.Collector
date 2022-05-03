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
        private readonly Dictionary<string, Tuple<HttpStatusCode, string>> requestToResponseMap;

        public FixedHttpClient()
        {
            this.requestToResponseMap = new Dictionary<string, Tuple<HttpStatusCode, string>>();
        }

        public void Reset()
        {
            this.requestToResponseMap.Clear();
        }

        public void AddResponse(string requestUrl, HttpStatusCode responseStatusCode, string responseMessage)
        {
            this.requestToResponseMap.Add(requestUrl, Tuple.Create(responseStatusCode, responseMessage));
        }

        public void AddResponse(string requestUrl, string requestBody, HttpStatusCode responseStatusCode, string responseMessage)
        {
            this.requestToResponseMap.Add(requestUrl + requestBody, Tuple.Create(responseStatusCode, responseMessage));
        }

        public Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication)
        {
            if (this.requestToResponseMap.TryGetValue(requestUrl, out Tuple<HttpStatusCode, string> response))
            {
                return Task.FromResult(new HttpResponseMessage()
                {
                    StatusCode = response.Item1,
                    Content = new StringContent(response.Item2),
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

        public Task<HttpResponseMessage> PostAsync(string requestUrl, IAuthentication authentication, string requestBody)
        {
            if (this.requestToResponseMap.TryGetValue(requestUrl + requestBody, out Tuple<HttpStatusCode, string> response))
            {
                HttpResponseMessage result = new HttpResponseMessage()
                {
                    StatusCode = response.Item1,
                    Content = new StringContent(response.Item2),
                };
                return Task.FromResult(result);
            }

            if (this.requestToResponseMap.TryGetValue(requestUrl, out response))
            {
                HttpResponseMessage result = new HttpResponseMessage()
                {
                    StatusCode = response.Item1,
                    Content = new StringContent(response.Item2),
                };
                return Task.FromResult(result);
            }

            throw new Exception($"FixedHttpClient: Unknown request '{requestUrl}'.");
        }
    }
}
