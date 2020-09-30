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
        /// Prepares the current record (a single row of the response) for outputting.
        /// </summary>
        public Func<JObject, JObject> PrepareRecordForOutput { get; set; } = record => record;
        /// <summary>
        /// Provides a way to potentially halt collection (stop batching) depending on the current record. This is helpful e.g., when the records are ordered by date and we want to only collect until some specific date.
        /// </summary>
        public Func<JObject, bool> HaltCollection { get; set; } = record => false;

        public List<HttpResponseSignature> AllowlistedResponses = new List<HttpResponseSignature>();

        [ObsoleteAttribute("This property is deprecated and will be removed in the next revision for compliance reasons. Use AllowlistedResponses instead.")]
        public List<HttpResponseSignature> WhitelistedResponses { get { return AllowlistedResponses; } set { AllowlistedResponses = WhitelistedResponses; } }


        public abstract object Clone();
    }
}
