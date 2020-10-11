// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Authentication;
using Microsoft.CloudMine.Core.Collectors.Error;
using Microsoft.CloudMine.Core.Collectors.IO;
using Microsoft.CloudMine.Core.Collectors.Telemetry;
using Microsoft.CloudMine.Core.Collectors.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Collector
{
    public abstract class CollectorBase<T> where T : CollectionNode
    {
        private readonly List<IRecordWriter> recordWriters;
        private readonly ITelemetryClient telemetryClient;
        private readonly IAuthentication authentication;

        private readonly bool enableLoopDetection;
        private List<string> previousRecordStrings;
        private int previousRecordCount;

        public CollectorBase(IAuthentication authentication, ITelemetryClient telemetryClient, IRecordWriter recordWriter = null)
        {
            this.authentication = authentication;
            this.telemetryClient = telemetryClient;

            this.recordWriters = new List<IRecordWriter>();
            if (recordWriter != null)
            {
                this.recordWriters.Add(recordWriter);
            }

            this.enableLoopDetection = true;
            this.previousRecordCount = -1;
            this.previousRecordStrings = null;
        }

        public CollectorBase(IAuthentication authentication, ITelemetryClient telemetryClient, List<IRecordWriter> recordWriters, bool enableLoopDetection = true)
        {
            this.authentication = authentication;
            this.telemetryClient = telemetryClient;
            this.recordWriters = recordWriters;

            this.enableLoopDetection = enableLoopDetection;
            this.previousRecordCount = 0;
            this.previousRecordStrings = new List<string>();
        }

        protected abstract IBatchingHttpRequest WrapIntoBatchingHttpRequest(T collectionNode);

        protected abstract Task<SerializedResponse> ParseResponseAsync(HttpResponseMessage response, T collectionNode);

        public Task ProcessAsync(T collectionNode)
        {
            return this.ProcessAsync(collectionNode, maxPageCount: long.MaxValue);
        }

        public void SetOutputPathPrefix(string outputPathPrefix)
        {
            foreach (IRecordWriter recordWriter in this.recordWriters)
            {
                recordWriter.SetOutputPathPrefix(outputPathPrefix);
            }
        }

        public async Task<bool> ProcessAsync(T collectionNode, long maxPageCount)
        {
            IBatchingHttpRequest batchingHttpRequest = this.WrapIntoBatchingHttpRequest(collectionNode);
            long counter = 0;
            bool haltCollection = false;
            while (batchingHttpRequest.HasNext && counter < maxPageCount && !haltCollection)
            {
                counter++;
                HttpResponseMessage response = await batchingHttpRequest.NextResponseAsync(this.authentication).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    SerializedResponse serializedResponse = await this.ParseResponseAsync(response, collectionNode).ConfigureAwait(false);
                    IEnumerable<JObject> records = serializedResponse.Records;
                    int recordCount = records.Count();
                    batchingHttpRequest.UpdateAvailability(serializedResponse.Response, recordCount);

                    // Check for looping. Majority of the requests won't have any batching at all, so start checking only after the very first call.
                    if (this.enableLoopDetection && counter > 1 && this.DetectLooping(records))
                    {
                        throw new FatalTerminalException($"Terminating activity due to loop detection during batching. The following requests resulted with the same response: '{batchingHttpRequest.PreviousUrl}', '{batchingHttpRequest.CurrentUrl}'.");
                    }

                    foreach (JObject record in records)
                    {
                        haltCollection = haltCollection || collectionNode.HaltCollection(record);
                        await this.ProcessRecordAsync(collectionNode, batchingHttpRequest, haltCollection, record).ConfigureAwait(false);
                    }

                    // recurse to children node of the response
                    List<CollectionNode> children = await collectionNode.ProduceChildrenFromResponseAsync(records, collectionNode.AdditionalMetadata).ConfigureAwait(false);
                    foreach (CollectionNode childCollectionNode in children)
                    {
                        T childNodeClone = (T)childCollectionNode.Clone();
                        // Add the context carried over the parent node. 
                        foreach (KeyValuePair<string, JToken> parentMetadataItem in collectionNode.AdditionalMetadata)
                        {
                            string parentMetadataName = parentMetadataItem.Key;
                            JToken parentMetadataValue = parentMetadataItem.Value;

                            // Do this only when it does not override existing context in the child node additional metadata.
                            // There is existing code that did not have this assumption, so for now, log potential overrides in telemetry so that we can clean them up in time.
                            if (childNodeClone.AdditionalMetadata.TryGetValue(parentMetadataName, out JToken metadataValue))
                            {
                                Dictionary<string, string> properties = new Dictionary<string, string>()
                                {
                                    { "ChildRecordType", childNodeClone.RecordType },
                                    { "ParentRecordType", collectionNode.RecordType },
                                    { "ApiName", collectionNode.ApiName },
                                    { "Name", parentMetadataName },
                                    { "Value", metadataValue.Value<string>() },
                                    { "NewValue", parentMetadataValue.Value<string>() },
                                };
                                this.telemetryClient.TrackEvent("IgnoredParentMetadata", properties);
                            }
                            else
                            {
                                childNodeClone.AdditionalMetadata.Add(parentMetadataName, parentMetadataValue);
                            }
                        }

                        await this.ProcessAsync(childNodeClone).ConfigureAwait(false);
                    }
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Dictionary<string, string> properties = new Dictionary<string, string>()
                    {
                        { "Url", batchingHttpRequest.CurrentUrl },
                        { "ResponseStatusCode", response.StatusCode.ToString() },
                        { "ResponseContent", responseContent },
                    };
                    this.telemetryClient.TrackEvent("FailedExternalRequest", properties);
                }
            }

            return batchingHttpRequest.HasNext;
        }

        private bool DetectLooping(IEnumerable<JObject> records)
        {
            List<string> recordStrings = new List<string>();
            foreach (JObject record in records)
            {
                recordStrings.Add(record.ToString(Formatting.None));
            }

            int recordCount = recordStrings.Count;
            if (recordCount != this.previousRecordCount)
            {
                // record counts do not match. Update previous pointers and return false.
                this.previousRecordCount = recordCount;
                this.previousRecordStrings = recordStrings;

                return false;
            }

            // number of records in the response is the same.
            for (int counter = 0; counter < recordStrings.Count; counter++)
            {
                if (!recordStrings[counter].Equals(this.previousRecordStrings[counter]))
                {
                    // record string at index 'counter' does not match (a different one). Update previous pointers and return false.
                    this.previousRecordCount = recordCount;
                    this.previousRecordStrings = recordStrings;

                    return false;
                }
            }

            // All records match to the previous record, incidcating that the response did not change either.
            return true;
        }

        private async Task ProcessRecordAsync(T collectionNode, IBatchingHttpRequest batchingHttpRequest, bool haltCollection, JObject record)
        {
            bool lastBatch = !(batchingHttpRequest.HasNext) || haltCollection;
            RecordContext context = new RecordContext()
            {
                RecordType = collectionNode.RecordType,
                AdditionalMetadata = new Dictionary<string, JToken>(collectionNode.AdditionalMetadata)
                {
                    // For OriginatingUril, use previous URL since the recording is done after the request is complete where the current URL has already become the next one.
                    { "OriginatingUrl", batchingHttpRequest.PreviousUrl },
                    { "LastBatch", lastBatch },
                },
            };

            string requestBody = collectionNode.RequestBody;
            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                context.AdditionalMetadata.Add("RequestBody", requestBody);
            }

            // Output the record itself.
            if (collectionNode.Output)
            {
                // It is extremely important that recordToOutput is computed and captured "before" the following foreach loop and the same reference is passed to WriteRecordAsync of each record writer.
                // If there is special processing of the record that e.g., creates a deep close of the original one to make modifications, otherwise, the metadata augmentation done by the first writer
                // is lost by the other writers.
                JObject recordToOutput = collectionNode.PrepareRecordForOutput(record);

                foreach (IRecordWriter recordWriter in this.recordWriters)
                {
                    await recordWriter.WriteRecordAsync(recordToOutput, context).ConfigureAwait(false);
                }
            }

            // Process the record and output additional records created during processing.
            List<RecordWithContext> additionalRecordsWithContext = await collectionNode.ProcessRecordAsync(record).ConfigureAwait(false);
            // We output additional records regardless "Output" is true or false. This permits us to discard the original response but transform into something different.
            foreach (RecordWithContext recordWithContext in additionalRecordsWithContext)
            {
                // For each additional record, augment their record context's additional metadata with the parent's record context's additional metadata.
                RecordContext additionalRecordContext = recordWithContext.Context;
                foreach (KeyValuePair<string, JToken> metadata in context.AdditionalMetadata)
                {
                    additionalRecordContext.AdditionalMetadata.Add(metadata.Key, metadata.Value);
                }

                foreach (IRecordWriter recordWriter in this.recordWriters)
                {
                    await recordWriter.WriteRecordAsync(recordWithContext.Record, additionalRecordContext).ConfigureAwait(false);
                }
            }

            // Recurse to the children collection nodes.
            Dictionary<string, JToken> newMetadata = collectionNode.ProduceAdditionalMetadata(record);
            List<CollectionNode> children = await collectionNode.ProduceChildrenAsync(record, collectionNode.AdditionalMetadata).ConfigureAwait(false);
            foreach (CollectionNode childCollectionNode in children)
            {
                T childNodeClone = (T)childCollectionNode.Clone();
                foreach (KeyValuePair<string, JToken> newMetadataItem in newMetadata)
                {
                    childNodeClone.AdditionalMetadata.Add(newMetadataItem.Key, newMetadataItem.Value);
                }

                // Add the context carried over the parent node. 
                foreach (KeyValuePair<string, JToken> parentMetadataItem in collectionNode.AdditionalMetadata)
                {
                    string parentMetadataName = parentMetadataItem.Key;
                    JToken parentMetadataValue = parentMetadataItem.Value;

                    // Do this only when it does not override existing context in the child node additional metadata.
                    // There is existing code that did not have this assumption, so for now, log potential overrides in telemetry so that we can clean them up in time.
                    if (childNodeClone.AdditionalMetadata.TryGetValue(parentMetadataName, out JToken metadataValue))
                    {
                        Dictionary<string, string> properties = new Dictionary<string, string>()
                        {
                            { "ChildRecordType", childNodeClone.RecordType },
                            { "ParentRecordType", collectionNode.RecordType },
                            { "ApiName", collectionNode.ApiName },
                            { "Name", parentMetadataName },
                            { "Value", metadataValue.Value<string>() },
                            { "NewValue", parentMetadataValue.Value<string>() },
                        };
                        this.telemetryClient.TrackEvent("IgnoredParentMetadata", properties);
                    }
                    else 
                    {
                        childNodeClone.AdditionalMetadata.Add(parentMetadataName, parentMetadataValue);
                    }
                }

                await this.ProcessAsync(childNodeClone).ConfigureAwait(false);
            }
        }
    }
}
