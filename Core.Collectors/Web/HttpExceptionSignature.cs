// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class HttpExceptionSignature
    {
        public static readonly HttpExceptionSignature RequestTimeoutException = new HttpExceptionSignature(exception => 
        {
            Type exceptionType = exception.GetType();
            return exceptionType == typeof(TaskCanceledException) && exception.Message.Equals("The operation was canceled.");
        });

        private readonly Func<Exception, bool> matcher;

        public HttpExceptionSignature(Func<Exception, bool> matcher)
        {
            this.matcher = matcher;
        }

        public bool Matches(Exception exception)
        {
            return matcher.Invoke(exception);
        }
    }
}
