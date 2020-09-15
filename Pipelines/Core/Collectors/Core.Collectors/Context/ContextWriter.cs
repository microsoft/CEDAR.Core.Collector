// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Microsoft.CloudMine.Core.Collectors.Context
{
    public abstract class ContextWriter<T> where T : FunctionContext
    {
        public abstract void AugmentMetadata(JObject metadata, T functionContext);
    }
}
