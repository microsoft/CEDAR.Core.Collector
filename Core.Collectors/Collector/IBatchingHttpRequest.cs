// Copyright (c) Microsoft Corporation. All rights reserved.

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

        Task<HttpResponseMessage> NextResponseAsync(IAuthentication authentication);
        void UpdateAvailability(JObject response, int recordCount);
    }
}
