// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.ApplicationInsights;
using Microsoft.CloudMine.Core.Collectors.Authentication;
using Microsoft.CloudMine.Core.Collectors.Error;
using Microsoft.CloudMine.Core.Collectors.Telemetry;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Collectors.Config
{
    public class ConfigManager
    {
        private const string DefaultKey = "*";

        private TelemetryClient telemetryClient;
        private readonly Dictionary<string, Dictionary<string, string>> telemetryEvents;
        private readonly JObject config;
        private readonly JToken apiDomainToken;
        private readonly Dictionary<string, JToken> authenticationTokenMap;
        private readonly Dictionary<string, JArray> recordWriterTokensMap;
        private readonly bool settingsFound;

        protected IConfigValueResolver ConfigResolver { private set; get; }

        public ConfigManager(string jsonString, IConfigValueResolver configResolver = null)
        {
            this.authenticationTokenMap = new Dictionary<string, JToken>();
            this.recordWriterTokensMap = new Dictionary<string, JArray>();
            this.telemetryEvents = new Dictionary<string, Dictionary<string, string>>();
            this.ConfigResolver = configResolver ?? new EnvironmentConfigValueResolver();
            this.settingsFound = jsonString != null;

            if (!this.settingsFound)
            {
                // specified Settings.json file could not be found
                return;
            }

            this.config = JObject.Parse(jsonString);
            this.apiDomainToken = config.SelectToken("ApiDomain");
            JToken defaultAuth = config.SelectToken("Authentication");
            this.authenticationTokenMap.Add(DefaultKey, defaultAuth);
            try
            {
                JArray defaultStorage = (JArray)this.config.SelectToken("Storage");
                this.recordWriterTokensMap.Add(DefaultKey, defaultStorage);
            }
            catch (Exception)
            {
                // no valid defualt storage configuration is provided, event will be tracked when telemetry client is added
                Dictionary<string, string> properties = new Dictionary<string, string>()
                {
                    { "ConfigName", "Storage" },
                    { "ExpectedFormat", "Storage : [ <Storage Types> ]"}
                };
                this.telemetryEvents.Add("Bad Config", properties);
            }

            JToken collectors = this.config.SelectToken("Collectors");
            if (collectors == null)
            {
                //no collectors provided - defualt values will be used for all collectors
                return;
            }

            foreach (JProperty token in collectors.Children())
            {
                string collectorType = token.Name;
                JToken collectorAuthToken = collectors.SelectToken(collectorType).SelectToken("Authentication");
                if (collectorAuthToken != null)
                {
                    this.authenticationTokenMap.Add(collectorType, collectorAuthToken);
                }

                JArray collectorStorageToken = (JArray)collectors.SelectToken(collectorType).SelectToken("Storage");
                if (collectorStorageToken != null)
                {
                    this.recordWriterTokensMap.Add(collectorType, collectorStorageToken);
                }
            }
        }

        protected void AddTelemetryEvent(string name, Dictionary<string, string> properties)
        {
            this.telemetryEvents.Add(name, properties);
        }

        protected JToken SelectConfigToken(string jsonPath)
        {
            ValidateSettingsExist();

            return this.config.SelectToken(jsonPath);
        }

        private void ValidateSettingsExist()
        {
            if (!this.settingsFound)
            {
                Exception exception = new FatalTerminalException("The specified Settings.json file could not be found");
                telemetryClient.TrackException(exception);
                throw exception;
            }
        }

        public void SetTelemetryClient(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;

            foreach (string telemetryEventKey in this.telemetryEvents.Keys)
            {
                // track events recorded before telemetry client was available
                this.telemetryClient.TrackEvent(telemetryEventKey, this.telemetryEvents[telemetryEventKey]);
            }
        }

        public virtual IAuthentication GetAuthentication(string collectorType)
        {
            ValidateSettingsExist();

            JToken authenticationToken = this.GetAuthenticationToken(collectorType);
            JToken authenticationTypeToken = authenticationToken.SelectToken("Type");
            if (authenticationTypeToken == null)
            {
                throw new FatalTerminalException($"For '{collectorType}' collector, Settings.json must provide a Type for authentication.");
            }

            string authenticationType = authenticationTypeToken.Value<string>();
            switch (authenticationType)
            {
                case "Basic":
                    JToken identityToken = authenticationToken.SelectToken("Identity");
                    JToken personalAccessTokenEnvironmentVariableToken = authenticationToken.SelectToken("PersonalAccessTokenEnvironmentVariable");
                    if (identityToken == null || personalAccessTokenEnvironmentVariableToken == null)
                    {
                        throw new FatalTerminalException($"For '{collectorType}' collector, Basic authentication requires 'Identity' and 'PersonalAccessTokenEnvironmentVariable' config values.");
                    }

                    string identity = identityToken.Value<string>();
                    string personalAccessToken = this.ConfigResolver.ResolveConfigValue(personalAccessTokenEnvironmentVariableToken.Value<string>());
                    return new BasicAuthentication(identity, personalAccessToken);
                
                default:
                    return null;
            }
        }

        protected JToken GetAuthenticationToken(string collectorType)
        {
            ValidateSettingsExist();

            if (!this.authenticationTokenMap.TryGetValue(collectorType, out JToken authenticationToken))
            {
                authenticationToken = this.authenticationTokenMap[DefaultKey];
            }
            return authenticationToken;
        }

        public string GetApiDomain()
        {
            ValidateSettingsExist();
            string apiDomain = string.Empty;
            try
            {
                apiDomain = this.apiDomainToken.Value<string>();
            }
            catch (Exception)
            {
                throw new FatalTerminalException($"Invalid URI: The hostname could not be parsed for API domain {apiDomainToken}. The API domain must be provided in Settings.json.");
            }
            return apiDomain;
        }

        public StorageManager GetStorageManager(string collectorType, ITelemetryClient telemetryClient)
        {
            ValidateSettingsExist();

            if (!this.recordWriterTokensMap.TryGetValue(collectorType, out JArray recordWritersArray) && !this.recordWriterTokensMap.TryGetValue(DefaultKey, out recordWritersArray))
            {
                throw new FatalTerminalException($"For '{collectorType}' collector, no default storage or collector specific storage configuration is provided in Settings.json");
            }

            return this.GetStorageManagerInternal(recordWritersArray, telemetryClient);
        }

        protected virtual StorageManager GetStorageManagerInternal(JArray recordWritersArray, ITelemetryClient telemetryClient)
        {
            return new StorageManager(recordWritersArray, telemetryClient);
        }
    }
}
