using Microsoft.CloudMine.Core.Collectors.Collector;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public interface  IAllowListStatus
    {

        public List<CollectionNode> Continuation();
    }
}
