// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class SerializedResponse
    {
        public JObject Response { get; }
        public IEnumerable<JObject> Records { get; }

        public SerializedResponse(JObject response, IEnumerable<JObject> records)
        {
            this.Response = response;
            this.Records = records;
        }
    }
}
