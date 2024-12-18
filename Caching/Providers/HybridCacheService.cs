using Caching.Interfaces;
using Caching.Options;
using Caching.Providers;
using Caching.Security;
using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

public class HybridCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ICacheService _distributedCache;
    private readonly Counter<int> _memoryCacheHitCounter;
    private readonly Counter<int> _memoryCacheMissCounter;
    private readonly Counter<int> _distributedCacheHitCounter;
    private readonly Counter<int> _distributedCacheMissCounter;
    private readonly bool _metricsEnabled;
    private readonly AesEncryptionService _encryptionService;

    public HybridCacheService(IMemoryCache memoryCache, ICacheService distributedCache, CacheOptions options, MeterProvider meterProvider = null, AesEncryptionService encryptionService = null)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _encryptionService = encryptionService;
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
        string serializedData;

        if (_encryptionService != null)
        {
            // Şifreleme varsa veriyi serialize edip şifrele
            var redisService = _distributedCache as RedisCacheService;
            serializedData = _encryptionService.Encrypt(redisService != null
                ? redisService.Serializer.Serialize(value)
                : value.ToString());
        }
        else
        {
            // Şifreleme yoksa sadece serialize et
            var redisService = _distributedCache as RedisCacheService;
            serializedData = redisService != null
                ? redisService.Serializer.Serialize(value)
                : value.ToString();
        }

        // Memory Cache'e yaz
        _memoryCache.Set(key, serializedData, expiry);

        // Distributed Cache'e yaz
        await _distributedCache.SetAsync(key, value, expiry, policy);
    }

    public async Task<T> GetAsync<T>(string key)
    {
        // Memory Cache'ten veri al
        if (_memoryCache.TryGetValue(key, out object memoryData))
        {
            if (_metricsEnabled) _memoryCacheHitCounter?.Add(1);

            // Şifre çözme işlemi
            var decryptedData = _encryptionService != null
                ? _encryptionService.Decrypt(memoryData.ToString())
                : memoryData.ToString();

            return typeof(T) == typeof(string) ? (T)(object)decryptedData : (T)Convert.ChangeType(decryptedData, typeof(T));
        }

        if (_metricsEnabled) _memoryCacheMissCounter?.Add(1);

        // Distributed Cache'ten veri al
        var distributedData = await _distributedCache.GetAsync<T>(key);
        if (distributedData != null)
        {
            if (_metricsEnabled) _distributedCacheHitCounter?.Add(1);

            // Memory Cache'e ekle
            _memoryCache.Set(key, distributedData, TimeSpan.FromMinutes(5));
            return distributedData;
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
