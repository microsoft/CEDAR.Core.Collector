// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Authentication;
using Microsoft.CloudMine.Core.Collectors.Collector;
using Microsoft.CloudMine.Core.Collectors.Tests.Authentication;
using Microsoft.CloudMine.Core.Collectors.Tests.IO;
using Microsoft.CloudMine.Core.Collectors.Tests.Telemetry;
using Microsoft.CloudMine.Core.Collectors.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.IO.Tests
{
    [TestClass]
    public class CollectorTests
    {
        [TestMethod]
        public async Task ProcessRecordAsync()
        {
            string response = @"{}";

            JObject record1 = JObject.Parse(@"{ ""value"": ""This is the first record."" }");
            RecordContext recordContext1 = new RecordContext()
            {
                AdditionalMetadata = new Dictionary<string, JToken>(),
                RecordType = "RecordType1",
            };

            JObject record2 = JObject.Parse(@"{ ""value"": ""This is the second record."" }");
            RecordContext recordContext2 = new RecordContext()
            {
                AdditionalMetadata = new Dictionary<string, JToken>(),
                RecordType = "RecordType2",
            };

            string url = "InitialUrl";

            MockCollectionNode collectionNode = new MockCollectionNode()
            {
                GetInitialUrl = metadata => url,
                Output = false,
                ProcessRecordAsync = record =>
                {
                    return Task.FromResult(new List<RecordWithContext>()
                    {
                        new RecordWithContext(record1, recordContext1),
                    });
                },
                ProcessRecordWithResponseAsync = (response, record) =>
                {
                    return Task.FromResult(new List<RecordWithContext>()
                    {
                        new RecordWithContext(record2, recordContext2),
                    });
                },
            };

            InMemoryRecordWriter recordWriter = new InMemoryRecordWriter();
            MockCollector mockCollector = new MockCollector(recordWriter, response);
            await mockCollector.ProcessAsync(collectionNode).ConfigureAwait(false);

            List<Tuple<JObject, RecordContext>> records = recordWriter.GetRecords();
            Assert.AreEqual(2, records.Count);

            (JObject record, RecordContext context) = records[0];
            Assert.AreEqual(record1.ToString(Formatting.None), record.ToString(Formatting.None));
            Assert.AreEqual(recordContext1.RecordType, context.RecordType);

            (record, context) = records[1];
            Assert.AreEqual(record2.ToString(Formatting.None), record.ToString(Formatting.None));
            Assert.AreEqual(recordContext2.RecordType, context.RecordType);
        }
    }

    internal class MockCollectionNode : CollectionNode
    {
        public override object Clone()
        {
            return new MockCollectionNode()
            {
                AllowlistedExceptions = this.AllowlistedExceptions,
                AdditionalMetadata = this.AdditionalMetadata,
                AllowlistedResponses = this.AllowlistedResponses,
                ApiName = this.ApiName,
                GetInitialUrl = this.GetInitialUrl,
                Output = this.Output,
                HaltCollection = this.HaltCollection,
                HaltCollectionFromResponse = this.HaltCollectionFromResponse,
                PrepareRecordForOutput = this.PrepareRecordForOutput,
                ProcessRecordAsync = this.ProcessRecordAsync,
                ProcessRecordWithResponseAsync = this.ProcessRecordWithResponseAsync,
                ProduceAdditionalMetadata = this.ProduceAdditionalMetadata,
                ProduceChildrenAsync = this.ProduceChildrenAsync,
                ProduceChildrenFromResponseAsync = this.ProduceChildrenFromResponseAsync,
                RecordType = this.RecordType,
                RequestBody = this.RequestBody,
                ResponseType = this.ResponseType,
                RetryRules = this.RetryRules,
            };
        }
    }

    internal class MockCollector : CollectorBase<MockCollectionNode>
    {
        private readonly string response;

        public MockCollector(InMemoryRecordWriter recordWriter, string response)
            : base(new NoopAuthentication(), new NoopTelemetryClient(), new List<IRecordWriter>() { recordWriter }, enableLoopDetection: false)
        {
            this.response = response;
        }

        protected override async Task<SerializedResponse> ParseResponseAsync(HttpResponseMessage response, MockCollectionNode collectionNode)
        {
            string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            JObject responseObject = JObject.Parse(responseContent);
            return new SerializedResponse(responseObject, new List<JObject>() { responseObject });
        }

        protected override IBatchingHttpRequest WrapIntoBatchingHttpRequest(MockCollectionNode collectionNode)
        {
            string url = collectionNode.GetInitialUrl(new Dictionary<string, JToken>());
            return new MockBatchingHttpRequest(url, this.response);
        }
    }

    internal class MockBatchingHttpRequest : IBatchingHttpRequest
    {
        private readonly string url;
        private readonly string response;

        private int counter;

        public MockBatchingHttpRequest(string url, string response)
        {
            this.url = url;
            this.response = response;

            this.counter = 0;
        }

        public bool HasNext
        {
            get
            {
                bool result = counter == 0;
                counter++;
                return result;
            }
        }

        public string CurrentUrl => this.url;

        public string PreviousUrl => this.url;

        public string PreviousIdentity => "Identity";

        public Task<RequestResult> NextResponseAsync(IAuthentication authentication)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(this.response),
            };
            return Task.FromResult(new RequestResult(responseMessage));
        }

        public void UpdateAvailability(JObject response, int recordCount)
        {
            // Assume success.
        }
    }
}
