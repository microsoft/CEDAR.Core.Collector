// Copyright (c) Microsoft Corporation. All rights reserved.

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

        public static async Task<JArray> ParseAsJArrayAsync(HttpResponseMessage response)
        {
            using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using StreamReader streamReader = new StreamReader(responseStream);
            using JsonTextReader jsonTextReader = new JsonTextReader(streamReader);
            return JArray.Load(jsonTextReader);
        }
    }
}
