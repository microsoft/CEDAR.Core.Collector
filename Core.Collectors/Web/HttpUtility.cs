// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public static class HttpUtility
    {
        public static async Task<JObject> ParseAsJObjectAsync(HttpResponseMessage response)
        {
            using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using StreamReader streamReader = new StreamReader(responseStream);
            using JsonTextReader jsonTextReader = new JsonTextReader(streamReader);
            return JObject.Load(jsonTextReader);
        }

        public static async Task<Tuple<JObject, long>> TryParseAsJObjectAsync(HttpResponseMessage response, long maxResponseLength)
        {
            using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            long responseLength = responseStream.Length;
            if (responseLength > maxResponseLength)
            {
                return Tuple.Create((JObject)null, responseLength);
            }

            using StreamReader streamReader = new StreamReader(responseStream);
            using JsonTextReader jsonTextReader = new JsonTextReader(streamReader);
            return Tuple.Create(JObject.Load(jsonTextReader), responseLength);
        }

        public static async Task<JArray> ParseAsJArrayAsync(HttpResponseMessage response)
        {
            using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using StreamReader streamReader = new StreamReader(responseStream);
            using JsonTextReader jsonTextReader = new JsonTextReader(streamReader);
            return JArray.Load(jsonTextReader);
        }
    }
}
