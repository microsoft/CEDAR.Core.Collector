// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public static async Task<JObject> TryParseAsJObjectAsync(HttpResponseMessage response, long maxResponseLength)
        {
            using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            if (responseStream.Length > maxResponseLength)
            {
                return null;
            }

            using StreamReader streamReader = new StreamReader(responseStream);
            using JsonTextReader jsonTextReader = new JsonTextReader(streamReader);
            return JObject.Load(jsonTextReader);
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
