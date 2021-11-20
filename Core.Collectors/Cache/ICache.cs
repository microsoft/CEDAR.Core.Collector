using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public interface ICache<T> where T : IContext
    {
        Task InitializeAsync();
        Task CacheAsync(T tableEntity);
        Task<bool> CacheAtomicAsync(T currentTableEntity, T newTableEntity);
        Task<T> RetrieveAsync(T tableEntity);
        Task<bool> ExistsAsync(T tableEntity);
    }
}
