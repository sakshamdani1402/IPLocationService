
namespace Location.Interfaces.Services
{
    public interface ICacheService
    {
        Task<T> GetFromCacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration) where T : class;
        Task<bool> StoreToCacheAsync<T>(string key, T value, TimeSpan expiration) where T : class;
    }
}

