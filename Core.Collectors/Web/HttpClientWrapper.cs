// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Authentication;
using Microsoft.CloudMine.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication)
        {
            return this.GetAsync(requestUrl, authentication, productInfoHeaderValue: null);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue)
        {
            return this.GetAsync(requestUrl, authentication, productInfoHeaderValue, eTag: string.Empty);
        }

        public Task<HttpResponseMessage> PostAsync(string requestUrl, string requestBody, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue)
        {
            return this.MakeRequestAsync(requestUrl, HttpMethod.Post, requestBody, authentication, productInfoHeaderValue, eTag: string.Empty);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue, string eTag)
        {
            return this.MakeRequestAsync(requestUrl, HttpMethod.Get, requestBody: string.Empty, authentication, productInfoHeaderValue, eTag);
        }

        private async Task<HttpResponseMessage> MakeRequestAsync(string requestUrl, HttpMethod method, string requestBody, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue, string eTag)
        {
            using Activity requestTrace = OpenTelemetryTracer.GetActivity(OpenTelemetryTrace.Request).Start();
            requestTrace.AddTag("Url", requestUrl);
            requestTrace.AddTag("Method", method);
            requestTrace.AddTag("AuthenticationType", authentication.GetType().ToString());

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
            string authenticationHeader = await authentication.GetAuthorizationHeaderAsync().ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue(authentication.Schema, authenticationHeader);
            foreach (KeyValuePair<string, string> additionalHeader in authentication.AdditionalWebRequestHeaders)
            {
                request.Headers.Add(additionalHeader.Key, additionalHeader.Value);
            }
            if (productInfoHeaderValue != null)
            {
                request.Headers.UserAgent.Add(productInfoHeaderValue);
            }
            if (!string.IsNullOrEmpty(eTag))
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(eTag));
            }

            try
            {
                HttpResponseMessage result = await this.httpClient.SendAsync(request).ConfigureAwait(false);
                requestTrace.AddTag("StatusCode", result.StatusCode);
                requestTrace.AddTag("Error", false);
                return result;
            }
            catch (Exception e)
            {
                requestTrace.AddTag("ExceptionType", e.GetType().ToString());
                requestTrace.AddTag("ExceptionMessage", e.Message);
                requestTrace.AddTag("Error", true);
                throw;
            }
            finally
            {
                TimeSpan requestDuration = requestTrace.Duration;
                requestTrace.AddTag("Duration", requestDuration);
            }
        }
    }
}
