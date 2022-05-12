// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Collector;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class HttpExceptionSignature : IAllowListStatus
    {
        public static HttpExceptionSignature RequestTimeoutException(Func<HttpRequestMessage, List<CollectionNode>> continuation = null)
        {
            static bool matcher(Exception exception)
            {
                Type exceptionType = exception.GetType();
                return exceptionType == typeof(TaskCanceledException) && exception.Message.Equals("The operation was canceled.");
            }
            return new HttpExceptionSignature(matcher, continuation);
        }

        public static HttpExceptionSignature SocketClosedException(Func<HttpRequestMessage, List<CollectionNode>> continuation = null)
        {
            static bool matcher(Exception exception)
            {
                Type exceptionType = exception.GetType();
                return exceptionType == typeof(SocketException) && exception.Message.Equals("An existing connection was forcibly closed by the remote host.");
            }
            return new HttpExceptionSignature(matcher, continuation);
        }

        public static HttpExceptionSignature FailedToParseResponseException(Func<HttpRequestMessage, List<CollectionNode>> continuation = null)
        {
            static bool matcher(Exception exception)
            {
                Type exceptionType = exception.GetType();
                return exceptionType == typeof(JsonReaderException) && exception.Message.StartsWith("Error reading JObject from JsonReader.");
            }
            return new HttpExceptionSignature(matcher, continuation);
        }

        private readonly Func<Exception, bool> matcher;
        private readonly Func<HttpRequestMessage, List<CollectionNode>> continuation;
        protected HttpExceptionSignature(Func<Exception, bool> matcher, Func<HttpRequestMessage, List<CollectionNode>> continuation = null)
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
