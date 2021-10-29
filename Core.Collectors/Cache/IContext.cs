using System.Collections.Generic;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public interface IContext
    {
        Dictionary<string, string> GetContext();
        void AddContext(string propertyName, string propertyValue);
    }
}
