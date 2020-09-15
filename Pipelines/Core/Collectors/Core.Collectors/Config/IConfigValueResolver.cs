// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.CloudMine.Core.Collectors.Config
{
    public interface IConfigValueResolver
    {
        string ResolveConfigValue(string configIdentifier);
    }

    public class EnvironmentConfigValueResolver : IConfigValueResolver
    {
        public string ResolveConfigValue(string configIdentifier)
        {
            return Environment.GetEnvironmentVariable(configIdentifier);
        }
    }
}
