// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.CloudMine.Core.Collectors.IO.Tests
{
    [TestClass]
    public class AzureHelpersTests
    {
        [TestMethod]
        public void GetContainerAndRelativePathFromMessage_Success()
        {
            string notificationMessage = @"AzureBlob: 1.0
{""BlobRecords"": [{""Path"": ""https://vstsanalyticswdgwus2.blob.core.windows.net/vstsanalytics/microsoft/OpStoreRestStream/v1/Hourly/2020/05/08/OpStoreRestStream_20200508_06_0004.json?sv=2018-03-28&sr=b&sig=shll%2FPUJ13VXeu8zdV1vU%2FFiACjZyXihDJ77EzTm%2Fcc%3D&st=2020-05-08T15%3A38%3A54Z&se=2020-05-15T15%3A38%3A54Z&sp=rl""}]}";

            Tuple<string, string> containerAndRelativePath = AzureHelpers.GetContainerAndRelativePathFromMessage(notificationMessage, "vstsanalyticswdgwus2");
            Assert.IsNotNull(containerAndRelativePath);
            Assert.AreEqual("vstsanalytics", containerAndRelativePath.Item1);
            Assert.AreEqual("microsoft/OpStoreRestStream/v1/Hourly/2020/05/08/OpStoreRestStream_20200508_06_0004.json", containerAndRelativePath.Item2);
        }

        [TestMethod]
        public void GetContainerAndRelativePathFromMessage_Failure()
        {
            string notificationMessage = @"AzureBlob: 1.0
{""BlobRecords"": [{""Path"": ""https://vstsanalyticswdgwus2.blob.core.windows.net/vstsanalytics/microsoft/OpStoreRestStream/v1/Hourly/2020/05/08/OpStoreRestStream_20200508_06_0004.json?sv=2018-03-28&sr=b&sig=shll%2FPUJ13VXeu8zdV1vU%2FFiACjZyXihDJ77EzTm%2Fcc%3D&st=2020-05-08T15%3A38%3A54Z&se=2020-05-15T15%3A38%3A54Z&sp=rl""}]}";

            Tuple<string, string> containerAndRelativePath = AzureHelpers.GetContainerAndRelativePathFromMessage(notificationMessage, "vstsanalyticssharedwus2");
            Assert.IsNull(containerAndRelativePath);
        }
    }
}
