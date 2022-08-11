using Microsoft.CloudMine.Core.Collectors.Authentication;
using Microsoft.CloudMine.Core.Collectors.Tests.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Web
{
    [TestClass()]
    public class FixedHttpClientTests
    {
        [TestMethod]
        public async Task TestFixedHttpClient()
        {
            string url = "testUrl";
            string responseContent = "testResponse";
            FixedHttpClient httpClient = new FixedHttpClient();
            IAuthentication authentication = new NoopAuthentication();
            httpClient.AddResponse(url, HttpStatusCode.OK, responseContent);

            // test that content can be read from 2 identical requests to FixedHttpClient.
            HttpResponseMessage responseMessage1 = await httpClient.GetAsync(url, authentication).ConfigureAwait(false);
            Stream contentStream1 = await responseMessage1.Content.ReadAsStreamAsync().ConfigureAwait(false);
            byte[] buffer1 = new byte[contentStream1.Length];
            contentStream1.Read(buffer1, 0, (int)contentStream1.Length);
            string contentString1 = Encoding.UTF8.GetString(buffer1);
            Assert.AreEqual(responseContent, contentString1);

            HttpResponseMessage responseMessage2 = await httpClient.GetAsync(url, authentication).ConfigureAwait(false);
            Stream contentStream2 = await responseMessage2.Content.ReadAsStreamAsync().ConfigureAwait(false);
            byte[] buffer2 = new byte[contentStream2.Length];
            contentStream2.Read(buffer2, 0, (int)contentStream2.Length);
            string contentString2 = Encoding.UTF8.GetString(buffer2);
            Assert.AreEqual(responseContent, contentString2);
        }
    }
}
