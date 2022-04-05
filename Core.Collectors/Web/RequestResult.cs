// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Net.Http;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class RequestResult
    {
        public HttpRequestMessage request { get; private set; }
        public HttpResponseMessage response { get; private set; }
        public IAllowListStatus allowListStatus { get; private set; }

        public RequestResult(HttpResponseMessage response)
                : this(response.RequestMessage, response, null)
        { }


        public RequestResult(HttpRequestMessage requestMessage, IAllowListStatus allowListStatus)
                : this(requestMessage, null, allowListStatus)
        { }

        private RequestResult(HttpRequestMessage request, HttpResponseMessage response, IAllowListStatus allowListStatus)
        {
            this.request = request;
            this.response = response;
            this.allowListStatus = allowListStatus;
        }

        public bool IsSuccess()
        {
            return this.response != null;
        }
    }
}
