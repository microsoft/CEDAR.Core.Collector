using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public interface ICache<T> where T : TableEntityWithContext
    {
        Task InitializeAsync();
        Task CacheAsync(T tableEntity);
        Task<T> RetrieveAsync(T tableEntity);
        Task<bool> ExistsAsync(T tableEntity);
    }
}
