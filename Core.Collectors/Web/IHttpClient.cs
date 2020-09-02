// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.CloudMine.Core.Collectors.Authentication;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication);
        Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue);
        Task<HttpResponseMessage> PostAsync(string requestUrl, string requestBody, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue);
        Task<HttpResponseMessage> GetAsync(string requestUrl, IAuthentication authentication, ProductInfoHeaderValue productInfoHeaderValue, string eTag);
    }
}
