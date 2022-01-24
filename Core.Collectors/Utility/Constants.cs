// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Collectors.Utility
{
    public static class Constants
    {
        public const string AppTokenAuth = "AppTokenAuth";
        public const string SeperateExtensionScopes = "SeperateExtensionScopes";
        public const string GrantType = "grant_type";
        public const string ClientId = "client_id";
        public const string ClientSecret = "client_secret";
        public const string ClientCredentialsGrantType = "client_credentials";
        public const string AdoScopeInput = "scope";
        //public const string MerlinBotDevOrgId = "9d2b790e-f716-4851-9379-4150f3f4b61a";
        public static List<string> ToolsToCheck = new List<string>()
        { "AntiMalware", "APIScan", "Armory", "Bandit", "Binskim", "CodeInspector", "CodesignValidation", "CredScan", "CSRF", "ESLint", "PoliCheck", "RoslynAnalyzers", "Semmle", "SDLNativeRules", "TSLint" };
    }

    public enum ExternalEventSource
    {
        AzureDevOps,
        Github
    }
}
