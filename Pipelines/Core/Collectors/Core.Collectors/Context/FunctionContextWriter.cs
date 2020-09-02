// Copyright (c) Microsoft Corporation. All rights reserved.

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
