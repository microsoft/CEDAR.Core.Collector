// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Config
{
    [TestClass]
    public class ConfigManagerTests
    {
        private ConfigManager configManager;

        [TestInitialize]
        public void Setup()
        {
            string jsonInput = @"
            {
                'Authentication' : {
                    'Type' : 'Basic',
                    'Identity' : 'msftgits',
                    'PersonalAccessTokenEnvironmentVariable' : 'PersonalAccessToken'
                },
                'Collectors' : {
                    'Main' : {}   
                },
                'ApiDomain':  'api.github.com'
            }";

            this.configManager = new ConfigManager(jsonInput);
        }
            [TestMethod]
        public void GetDefaultAuthentication()
        {
            Assert.IsNotNull(this.configManager.GetAuthentication("Main"));
        }

        [TestMethod]
        public void GetApiDomain()
        {
            Assert.AreEqual("api.github.com", configManager.GetApiDomain());
        }
    }
}
