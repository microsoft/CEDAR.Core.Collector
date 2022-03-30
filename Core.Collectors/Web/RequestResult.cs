﻿// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Net.Http;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class RequestResult
    {
        public HttpResponseMessage response;
        public IAllowListStatus allowListStatus;

        public RequestResult(HttpResponseMessage response)
                : this(response, null)
        { }


        public RequestResult(IAllowListStatus allowListStatus)
                : this(null, allowListStatus)
        { }

        private RequestResult(HttpResponseMessage response, IAllowListStatus allowListStatus)
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
