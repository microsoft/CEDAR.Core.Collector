// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Authentication
{
    public interface IAuthentication
    {
        Task<string> GetAuthorizationHeaderAsync();

        Dictionary<string, string> AdditionalWebRequestHeaders { get; }

        /// <summary>
        /// Identity is subsequently used to partition rate limiting. Make sure to return a value.
        /// </summary>
        string Identity { get; }

        string Schema { get; }
    }
}
