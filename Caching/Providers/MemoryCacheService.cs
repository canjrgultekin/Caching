using Caching.Interfaces;
using Caching.Options;
using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

namespace Caching.Providers;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly Counter<int> _cacheHitCounter;
    private readonly Counter<int> _cacheMissCounter;
    private readonly bool _metricsEnabled;

    public MemoryCacheService(IMemoryCache cache, CacheOptions options, MeterProvider meterProvider = null)
    {
        _cache = cache;
        _metricsEnabled = options.MetricsEnabled;

        if (_metricsEnabled && meterProvider != null)
        {
            var meter = new Meter("Caching.MemoryCache");
            _cacheHitCounter = meter.CreateCounter<int>("memory_cache_hits", "count", "Number of cache hits.");
            _cacheMissCounter = meter.CreateCounter<int>("memory_cache_misses", "count", "Number of cache misses.");
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiry, CachePolicy policy = null)
    {
        _cache.Set(key, value, expiry);
        return Task.CompletedTask;
    }

    public Task<T> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out T value))
        {
            if (_metricsEnabled) _cacheHitCounter?.Add(1);
            return Task.FromResult(value);
        }

        if (_metricsEnabled) _cacheMissCounter?.Add(1);
        return Task.FromResult(default(T));
    }

    public Task<bool> RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.FromResult(true);
    }
}
