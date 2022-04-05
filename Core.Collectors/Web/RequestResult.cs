// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Net.Http;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class RequestResult
    {
        public HttpRequestMessage request;
        public HttpResponseMessage response;
        public IAllowListStatus allowListStatus;

        public RequestResult(HttpRequestMessage requestMessage, HttpResponseMessage response)
                : this(requestMessage, response, null)
        { }


        public RequestResult(HttpRequestMessage requestMessage, IAllowListStatus allowListStatus)
                : this(requestMessage, null, allowListStatus)
        { }

        private RequestResult(HttpRequestMessage requestMessage, HttpResponseMessage response, IAllowListStatus allowListStatus)
        {
            this.response = response;
            this.allowListStatus = allowListStatus;
        }

        public bool IsSuccess()
        {
            return this.response != null;
        }
    }
}
