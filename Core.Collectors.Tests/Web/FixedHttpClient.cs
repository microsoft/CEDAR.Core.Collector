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
        private readonly Dictionary<string, Func<Tuple<HttpStatusCode, string>>> requestToResponseGeneratorMap;

        public FixedHttpClient()
        {
            this.requestToResponseMap = new Dictionary<string, Tuple<HttpStatusCode, string>>();
            this.requestToResponseGeneratorMap = new Dictionary<string, Func<Tuple<HttpStatusCode, string>>>();
        }

        public void Reset()
        {
            this.requestToResponseMap.Clear();
            this.requestToResponseGeneratorMap.Clear();
        }

        public void AddResponseGenerator(string requestUrl, Func<Tuple<HttpStatusCode, string>> resposneGenerator)
        {
            this.requestToResponseGeneratorMap.Add(requestUrl, resposneGenerator);
        }

        public void AddResponseGenerator(string requestUrl, string responseBody, Func<Tuple<HttpStatusCode, string>> resposneGenerator)
        {
            this.requestToResponseGeneratorMap.Add(requestUrl + responseBody, resposneGenerator);
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
    }
}
