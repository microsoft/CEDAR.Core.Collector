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
        public const long MB = 1024 * 1024;

        public static async Task<JObject> ParseAsJObjectAsync(HttpResponseMessage response)
        {
            using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using StreamReader streamReader = new StreamReader(responseStream);
            using JsonTextReader jsonTextReader = new JsonTextReader(streamReader);
            return JObject.Load(jsonTextReader);
        }

        public static async Task<Tuple<bool, JObject>> TryParseAsJObjectAsync(HttpResponseMessage response)
        {
            using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            // Loading a stream / text into JObject expands ~10x in memory. In other words, a 100MB response, will approx. take 1GB ram.
            // Our machines have 3.5 GB ram. I am leaving 1GB for OS (might not be needed but just in case) and the rest of the code.
            // That leaves 2.5 GB to be shared between 6, or ~400 MB each, so ~40 MB raw response.
            if (responseStream.Length > 40 * MB)
            {
                return Tuple.Create(false, (JObject)null);
            }

            using StreamReader streamReader = new StreamReader(responseStream);
            using JsonTextReader jsonTextReader = new JsonTextReader(streamReader);
            return Tuple.Create(true, JObject.Load(jsonTextReader));
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
