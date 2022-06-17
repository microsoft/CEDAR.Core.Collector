// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.IO;
using Microsoft.CloudMine.Core.Collectors.Web;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Collector
{
    // warning - this is ICloneable with a not perfect implementation of cloning - sooooooo consider yourself warned
    // if you add anything here remember about cloning in the inheriting classes...
    public abstract class CollectionNode : ICloneable
    {
        protected CollectionNode()
        {
        }

        public Dictionary<string, JToken> AdditionalMetadata { get; set; } = new Dictionary<string, JToken>();
        public Func<Dictionary<string, JToken>, string> GetInitialUrl { get; set; }
        public string RequestBody { get; set; } = string.Empty;
        public string RecordType { get; set; }
        public string ApiName { get; set; }
        public bool Output { get; set; } = true;
        public virtual Type ResponseType { get; set; }

        /// <summary>
        /// Produces a list (potentially empty) of children (collection nodes representing children requests) from the current record (a single row of the response).
        /// </summary>
        public Func<JObject, Dictionary<string, JToken>, Task<List<CollectionNode>>> ProduceChildrenAsync { get; set; } = (record, metadata) => Task.FromResult(new List<CollectionNode>());
        /// <summary>
        /// Produces a list (potentially empty) of children (collection nodes representing children requests) from the complete response.
        /// </summary>
        public Func<IEnumerable<JObject>, Dictionary<string, JToken>, Task<List<CollectionNode>>> ProduceChildrenFromResponseAsync { get; set; } = (records, metadata) => Task.FromResult(new List<CollectionNode>());
        /// <summary>
        /// Produces additional metadata (potentially empty, passed to the children nodes as well) from the current record (a single row of the response).
        /// </summary>
        public Func<JObject, Dictionary<string, JToken>> ProduceAdditionalMetadata { get; set; } = record => new Dictionary<string, JToken>();
        /// <summary>
        /// Produces a list of additional records (potentially empty, other than the record itself) from the current record (a single row of the response).
        /// </summary>
        public Func<JObject, Task<List<RecordWithContext>>> ProcessRecordAsync { get; set; } = record => Task.FromResult(new List<RecordWithContext>());
        /// <summary>
        /// Produces a list of additional records (potentially empty, other than the record itself) from the current record (a single row of the response) as well as the full response.
        /// </summary>
        public Func<JObject, JObject, Task<List<RecordWithContext>>> ProcessRecordWithResponseAsync { get; set; } = (response, record) => Task.FromResult(new List<RecordWithContext>());
        /// <summary>
        /// Prepares the current record (a single row of the response) for outputting.
        /// </summary>
        public Func<JObject, JObject> PrepareRecordForOutput { get; set; } = record => record;
        /// <summary>
        /// Provides a way to potentially halt collection (stop batching) depending on the current record. This is helpful e.g., when the records are ordered by date and we want to only collect until some specific date.
        /// </summary>
        public Func<JObject, bool> HaltCollection { get; set; } = record => false;
        /// <summary>
        /// Provides a way to potentially halt collection (stop batching) depending on the full API response. This is helpful e.g., when the response contains an information about existance of next batch.
        /// </summary>
        public Func<JObject, bool> HaltCollectionFromResponse { get; set; } = response => false;
        /// <summary>
        /// Provides a way to allow-list non-success HTTP responses.
        /// </summary>
        public List<HttpResponseSignature> AllowlistedResponses = new List<HttpResponseSignature>();
        /// <summary>
        /// Provides a way to allow-list HTTP exceptions.
        /// </summary>
        public List<HttpExceptionSignature> AllowlistedExceptions = new List<HttpExceptionSignature>();
        /// <summary>
        /// Additional retry rules that apply to this node only.
        /// </summary>
        public List<RetryRule> RetryRules = new List<RetryRule>();

        public abstract object Clone();
    }
}
