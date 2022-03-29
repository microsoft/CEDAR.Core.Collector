// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Collector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Web
{ 
    public class HttpExceptionSignature : IAllowListStatus
    {
        public static HttpExceptionSignature RequestTimeoutException(Func<List<CollectionNode>> continuation = null)
        {
            Func<Exception, bool> matcher = exception =>
            {
                Type exceptionType = exception.GetType();
                return exceptionType == typeof(TaskCanceledException) && exception.Message.Equals("The operation was canceled.");
            };

            return new HttpExceptionSignature(matcher, continuation);
        }

        private readonly Func<Exception, bool> matcher;
        private readonly Func<List<CollectionNode>> continuation;

        public HttpExceptionSignature(Func<Exception, bool> matcher, Func<List<CollectionNode>> continuation = null)
        {
            this.matcher = matcher;

            if (continuation == null)
            {
                continuation = () => new List<CollectionNode>();
            }

            this.continuation = continuation;
        }

        public bool Matches(Exception exception)
        {
            return matcher.Invoke(exception);
        }

        public List<CollectionNode> Continuation()
        {
            return this.continuation.Invoke();
        }
    }
}
