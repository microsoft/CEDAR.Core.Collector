// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Authentication
{
    public class BasicAuthentication : IAuthentication
    {
        private readonly string authorizationHeader;

        public string Identity { get; }
        public Dictionary<string, string> AdditionalWebRequestHeaders => new Dictionary<string, string>();
        public string Schema => "Basic";

        public BasicAuthentication(string identity, string personalAccessToken)
        {
            this.authorizationHeader = $"{(Convert.ToBase64String(Encoding.Default.GetBytes($"{identity}:{personalAccessToken}")))}";
            this.Identity = identity;
        }

        public Task<string> GetAuthorizationHeaderAsync()
        {
            return Task.FromResult(this.authorizationHeader);
        } 
    }
}
