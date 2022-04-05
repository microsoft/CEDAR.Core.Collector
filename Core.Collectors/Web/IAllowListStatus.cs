// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.CloudMine.Core.Collectors.Collector;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public interface  IAllowListStatus
    {
        public List<CollectionNode> Continuation(HttpRequestMessage failedRequest);
    }
}
