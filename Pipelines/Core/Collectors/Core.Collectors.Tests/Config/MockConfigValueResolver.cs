// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.CloudMine.Core.Collectors.Config;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Config
{
    public class MockConfigValueResolver : IConfigValueResolver
    {
        private readonly string configValue;

        public MockConfigValueResolver(string configValue)
        {
            this.configValue = configValue;
        }

        public string ResolveConfigValue(string configIdentifier)
        {
            return this.configValue;
        }
    }
}
