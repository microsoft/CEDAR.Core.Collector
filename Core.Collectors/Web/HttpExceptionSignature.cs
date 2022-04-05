// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Collector;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class HttpExceptionSignature : IAllowListStatus
    {
        public static HttpExceptionSignature RequestTimeoutException(Func<HttpRequestMessage, List<CollectionNode>> continuationNodeList = null)
        {
            Func<Exception, bool> matcher = (exception) =>
            {
                Type exceptionType = exception.GetType();
                return exceptionType == typeof(TaskCanceledException) && exception.Message.Equals("The operation was canceled.");
            };

            return new HttpExceptionSignature(matcher, continuationNodeList);
        }

        private readonly Func<Exception, bool> matcher;
        private readonly Func<HttpRequestMessage, List<CollectionNode>> continuation;
        public HttpExceptionSignature(Func<Exception, bool> matcher, Func<HttpRequestMessage, List<CollectionNode>> continuation = null)
        {
            this.matcher = matcher;
            this.continuation = continuation;
        }

        public bool Matches(Exception exception)
        {
            return matcher.Invoke(exception);
        }

        public List<CollectionNode> Continuation(HttpRequestMessage failedRequest)
        {
            if (continuation == null)
            {
                return new List<CollectionNode>();
            }

            return this.continuation(failedRequest);
        }
    }
}
