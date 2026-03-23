using Microsoft.Extensions.Caching.Memory;
using TaskForge.Application.Interfaces;

namespace TaskForge.Infrastructure.Services;

/// <summary>
/// In-memory caching service wrapping IMemoryCache.
/// Thread-safe: IMemoryCache is inherently thread-safe.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T? Get<T>(string key)
    {
        _cache.TryGetValue(key, out T? value);
        return value;
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration,
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };
        _cache.Set(key, value, options);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }
}
