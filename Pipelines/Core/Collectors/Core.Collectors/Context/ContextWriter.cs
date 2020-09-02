// Copyright (c) Microsoft Corporation. All rights reserved.

using Newtonsoft.Json.Linq;

namespace Microsoft.CloudMine.Core.Collectors.Context
{
    public abstract class ContextWriter<T> where T : FunctionContext
    {
        public abstract void AugmentMetadata(JObject metadata, T functionContext);
    }
}
