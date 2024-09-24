using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Location.Interfaces.Services;

namespace Location.Services.Cache
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T> GetFromCacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            }
            if (_cache.TryGetValue(key, out T cacheEntry))
            {
                return cacheEntry;
            }
            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();

            try
            {
                if (_cache.TryGetValue(key, out cacheEntry))
                {
                    return cacheEntry;
                }
                var newEntry = await factory();
                _cache.Set(key, newEntry, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                });

                return newEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while getting or creating cache entry for key '{key}': {ex.Message}", ex);
                return null;
            }
            finally
            {
                semaphore.Release();
                _locks.TryRemove(key, out _);
            }
        }

        public async Task<bool> StoreToCacheAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            try
            {
                _cache.Set(key, value, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                });

                return await Task.FromResult(true); // Indicate success
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while storing cache entry for key '{key}': {ex.Message}", ex);
                return await Task.FromResult(false); // Indicate failure
            }
        }

    }

}