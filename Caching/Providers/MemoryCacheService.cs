using Caching.Interfaces;
using Caching.Options;
using Caching.Security;
using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly Counter<int> _cacheHitCounter;
    private readonly Counter<int> _cacheMissCounter;
    private readonly bool _metricsEnabled;
    private readonly AesEncryptionService _encryptionService;

    public MemoryCacheService(IMemoryCache cache, CacheOptions options, MeterProvider meterProvider = null, AesEncryptionService encryptionService = null)
    {
        _cache = cache;
        _metricsEnabled = options.MetricsEnabled;
        _encryptionService = encryptionService;

        if (_metricsEnabled && meterProvider != null)
        {
            var meter = new Meter("Caching.MemoryCache");
            _cacheHitCounter = meter.CreateCounter<int>("memory_cache_hits", "count", "Number of cache hits.");
            _cacheMissCounter = meter.CreateCounter<int>("memory_cache_misses", "count", "Number of cache misses.");
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiry, CachePolicy policy = null)
    {
        var data = _encryptionService != null ? _encryptionService.Encrypt(value.ToString()) : value.ToString();
        _cache.Set(key, data, expiry);
        return Task.CompletedTask;
    }

    public Task<T> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out string data))
        {
            if (_metricsEnabled) _cacheHitCounter?.Add(1);
            var decryptedData = _encryptionService != null ? _encryptionService.Decrypt(data) : data;
            return Task.FromResult((T)Convert.ChangeType(decryptedData, typeof(T)));
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
