// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Config;
using System;
using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Config
{
    public class MockConfigValueResolver : IConfigValueResolver
    {
        private readonly Dictionary<string, string> configMap;

        public MockConfigValueResolver(Dictionary<string, string> configMap)
        {
            this.configMap = configMap;
        }

        public string ResolveConfigValue(string configIdentifier)
        {
            if (!this.configMap.TryGetValue(configIdentifier, out string result))
            {
                throw new InvalidOperationException($"MockConfigValueResolver does not have a value for '{configIdentifier}'.");
            }

            return result;
        }
    }
}
