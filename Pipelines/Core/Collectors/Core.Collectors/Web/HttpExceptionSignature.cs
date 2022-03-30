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
        public static readonly HttpExceptionSignature RequestTimeoutException = new HttpExceptionSignature(exception =>
        {
            Type exceptionType = exception.GetType();
            return exceptionType == typeof(TaskCanceledException) && exception.Message.Equals("The operation was canceled.");
        });

        private readonly Func<Exception, bool> matcher;
        private readonly Func<List<CollectionNode>> continuation;
        public HttpExceptionSignature(Func<Exception, bool> matcher, Func<List<CollectionNode>> continuation = null)
        {
            this.matcher = matcher;
            this.continuation = continuation;
        }

        public bool Matches(Exception exception)
        {
            return matcher.Invoke(exception);
        }

        public List<CollectionNode> Continuation()
        {
            if (continuation == null)
            {
                return new List<CollectionNode>();
            }

            return this.continuation();
        }
    }
}
