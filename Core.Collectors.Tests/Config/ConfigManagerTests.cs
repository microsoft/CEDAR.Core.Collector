// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.CloudMine.Core.Collectors.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Config
{
    [TestClass]
    public class ConfigManagerTests
    {
        [TestMethod]
        public void GetDefaultAuthentication()
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
                }
            }";

            ConfigManager configManager = new ConfigManager(jsonInput);
            Assert.IsNotNull(configManager.GetAuthentication("Main"));
        }
    }
}
