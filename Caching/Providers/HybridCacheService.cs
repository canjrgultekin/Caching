using Caching.Interfaces;
using Caching.Options;
using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

namespace Caching.Providers;

public class HybridCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ICacheService _distributedCache;

    private readonly Counter<int> _memoryCacheHitCounter;
    private readonly Counter<int> _memoryCacheMissCounter;
    private readonly Counter<int> _distributedCacheHitCounter;
    private readonly Counter<int> _distributedCacheMissCounter;

    private readonly bool _metricsEnabled;

    public HybridCacheService(IMemoryCache memoryCache, ICacheService distributedCache, CacheOptions options, MeterProvider meterProvider = null)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _metricsEnabled = options.MetricsEnabled;

        if (_metricsEnabled && meterProvider != null)
        {
            var meter = new Meter("Caching.HybridCache");

            _memoryCacheHitCounter = meter.CreateCounter<int>("hybrid_memory_cache_hits", "count", "Number of hits in MemoryCache.");
            _memoryCacheMissCounter = meter.CreateCounter<int>("hybrid_memory_cache_misses", "count", "Number of misses in MemoryCache.");
            _distributedCacheHitCounter = meter.CreateCounter<int>("hybrid_distributed_cache_hits", "count", "Number of hits in Distributed Cache.");
            _distributedCacheMissCounter = meter.CreateCounter<int>("hybrid_distributed_cache_misses", "count", "Number of misses in Distributed Cache.");
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CachePolicy policy = null)
    {
        _memoryCache.Set(key, value, expiry);
        await _distributedCache.SetAsync(key, value, expiry, policy);
    }

    public async Task<T> GetAsync<T>(string key)
    {
        if (_memoryCache.TryGetValue(key, out T value))
        {
            if (_metricsEnabled) _memoryCacheHitCounter?.Add(1);
            return value;
        }

        if (_metricsEnabled) _memoryCacheMissCounter?.Add(1);

        value = await _distributedCache.GetAsync<T>(key);
        if (value != null)
        {
            if (_metricsEnabled) _distributedCacheHitCounter?.Add(1);
            _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
            return value;
        }

        if (_metricsEnabled) _distributedCacheMissCounter?.Add(1);

        return default;
    }

    public async Task<bool> RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        return await _distributedCache.RemoveAsync(key);
    }
}
