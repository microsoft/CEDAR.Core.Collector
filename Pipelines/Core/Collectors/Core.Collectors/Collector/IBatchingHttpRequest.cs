// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Authentication;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Collector
{
    public interface IBatchingHttpRequest
    {
        bool HasNext { get; }
        string CurrentUrl { get; }
        string PreviousUrl { get; }
        string PreviousIdentity { get; }

        Task<HttpResponseMessage> NextResponseAsync(IAuthentication authentication);
        void UpdateAvailability(JObject response, int recordCount);
    }
}
