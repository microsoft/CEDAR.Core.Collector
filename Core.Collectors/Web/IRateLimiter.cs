// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Authentication;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public interface IRateLimiter
    {
        Task UpdateRetryAfterAsync(string identity, string requestUrl, HttpResponseMessage response);
        Task UpdateStatsAsync(string identity, string requestUrl, HttpResponseMessage response);
        Task WaitIfNeededAsync(IAuthentication authentication);
    }
}
