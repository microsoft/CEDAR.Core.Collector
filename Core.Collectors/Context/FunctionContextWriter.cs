// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Microsoft.CloudMine.Core.Collectors.Context
{
    public class FunctionContextWriter : ContextWriter<FunctionContext>
    {
        public override void AugmentMetadata(JObject metadata, FunctionContext functionContext)
        {
        }
    }
}
