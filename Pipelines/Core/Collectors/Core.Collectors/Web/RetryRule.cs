// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class RetryRule
    {
        public Func<HttpResponseMessage, Task<bool>> ShallRetryAsync { get; set; } = respose => Task.FromResult(false);
        public TimeSpan[] DelayBeforeRetries { get; set; } = new TimeSpan[0];
    }
}
