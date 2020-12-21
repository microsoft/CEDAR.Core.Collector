// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class RetryRule
    {
        public Func<HttpResponseMessage, Task<bool>> ShallRetryAsync { get; set; } = respose => Task.FromResult(false);
        public TimeSpan[] DelayBeforeRetries { get; set; } = new TimeSpan[0];

        public long AttemptIndex { get; private set; }

        public RetryRule()
        {
            this.AttemptIndex = 0;
        }

        public void Clear()
        {
            this.AttemptIndex = 0;
        }

        public void Consume()
        {
            this.AttemptIndex++;
        }
    }
}
