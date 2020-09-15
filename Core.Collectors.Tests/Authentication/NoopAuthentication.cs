// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Authentication;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Authentication
{
    public class NoopAuthentication : IAuthentication
    {
        public Dictionary<string, string> AdditionalWebRequestHeaders => new Dictionary<string, string>() { };

        public string Identity => "NoopAuthenticationIdentity";

        public string Schema => "NoopAuthenticationSchema";

        public Task<string> GetAuthorizationHeaderAsync()
        {
            return Task.FromResult("NoopAuthorizationHeader");
        }
    }
}
