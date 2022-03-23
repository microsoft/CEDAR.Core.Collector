// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class HttpExceptionSignature
    {
        private readonly Func<Exception, bool> matcher;
        private readonly Func<Task> handler;

        public HttpExceptionSignature(Func<Exception, bool> matcher, Func<Task> handler = null)
        {
            this.matcher = matcher;
            this.handler = handler;
        }

        public bool Matches(Exception exception)
        {
            return matcher.Invoke(exception);
        }

        public Task Handle()
        {
            if (null != this.handler)
            {
                return this.handler.Invoke();
            }
            return Task.CompletedTask;
        }

        public static HttpExceptionSignature RequestTimeoutException(Func<Task> handler = null)
        {
            Func<Exception, bool> matcher = exception => {
                Type exceptionType = exception.GetType();
                return exceptionType == typeof(TaskCanceledException) && exception.Message.Equals("The operation was canceled.");
            };

            return new HttpExceptionSignature(matcher, handler);
        }
    }
}
