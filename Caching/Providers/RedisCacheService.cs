using Caching.Interfaces;
using Caching.Options;
using Caching.Security;
using Caching.Serialization;
using OpenTelemetry.Metrics;
using StackExchange.Redis;
using System.Diagnostics.Metrics;

namespace Caching.Providers;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    public ISerializer Serializer { get; } // Dışarıya erişim için public özellik
    private readonly Counter<int> _cacheHitCounter;
    private readonly Counter<int> _cacheMissCounter;
    private readonly bool _metricsEnabled;
    private readonly AesEncryptionService _encryptionService;

    public RedisCacheService(CacheOptions options, ISerializer serializer, MeterProvider meterProvider = null, AesEncryptionService encryptionService = null)
    {
        var redis = ConnectionMultiplexer.Connect(options.ConnectionString);
        _database = redis.GetDatabase();
        Serializer = serializer; // Özellik olarak atanıyor
        _encryptionService = encryptionService;
        _metricsEnabled = options.MetricsEnabled;

        if (_metricsEnabled && meterProvider != null)
        {
            var meter = new Meter("Caching.RedisCache");
            _cacheHitCounter = meter.CreateCounter<int>("redis_cache_hits", "count", "Number of Redis cache hits.");
            _cacheMissCounter = meter.CreateCounter<int>("redis_cache_misses", "count", "Number of Redis cache misses.");
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CachePolicy policy = null)
    {
        var serialized = Serializer.Serialize(value);
        if (_encryptionService != null)
        {
            serialized = _encryptionService.Encrypt(serialized);
        }

        await _database.StringSetAsync(key, serialized, expiry);
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var data = await _database.StringGetAsync(key);
        if (data.HasValue)
        {
            if (_metricsEnabled) _cacheHitCounter?.Add(1);

            var decryptedData = _encryptionService != null ? _encryptionService.Decrypt(data) : data.ToString();
            return Serializer.Deserialize<T>(decryptedData);
        }

        if (_metricsEnabled) _cacheMissCounter?.Add(1);
        return default;
    }

    public async Task<bool> RemoveAsync(string key)
    {
        return await _database.KeyDeleteAsync(key);
    }
}

