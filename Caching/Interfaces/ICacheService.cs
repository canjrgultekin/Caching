using Caching.Options;

namespace Caching.Interfaces
{
    public interface ICacheService
    {
        Task SetAsync<T>(string key, T value, TimeSpan expiry, CachePolicy policy = null);
        Task<T> GetAsync<T>(string key);
        Task<bool> RemoveAsync(string key);
    }
}
