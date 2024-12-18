using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Caching.Interfaces;
using Caching.Metrics;
using Caching.Providers;
using Caching.Serialization;
using OpenTelemetry.Metrics;
using Caching.Options;
using Castle.DynamicProxy;
using Caching.Aspects; // Interceptor için gerekli

namespace Caching.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddAdvancedCaching(
        this IServiceCollection services,
        Action<CacheOptions> configureOptions,
        CacheProviderType providerType)
    {
        var options = new CacheOptions();
        configureOptions(options);

        MeterProvider meterProvider = null;
        if (options.MetricsEnabled)
        {
            meterProvider = MetricsProvider.ConfigureMetrics();
            services.AddSingleton(meterProvider);
        }

        services.AddSingleton<ISerializer, NewtonsoftJsonSerializer>();

        switch (providerType)
        {
            case CacheProviderType.Memory:
                // MemoryCache
                services.AddMemoryCache();
                services.AddSingleton<ICacheService>(sp =>
                    new MemoryCacheService(sp.GetRequiredService<IMemoryCache>(), options, meterProvider));
                break;

            case CacheProviderType.Redis:
                // Redis Cache
                services.AddSingleton<ICacheService>(sp =>
                    new RedisCacheService(options, new NewtonsoftJsonSerializer(), meterProvider));
                break;

            case CacheProviderType.Hybrid:
                // Hybrid Cache: MemoryCache + RedisCache
                services.AddMemoryCache();
                services.AddSingleton<ICacheService>(sp =>
                {
                    var memoryCache = sp.GetRequiredService<IMemoryCache>();

                    // RedisCacheService oluşturuluyor
                    var redisCache = new RedisCacheService(options, sp.GetRequiredService<ISerializer>(), meterProvider);

                    return new HybridCacheService(memoryCache, redisCache, options, meterProvider);
                });
                break;
        }

        // Interceptor entegrasyonu
        services.AddSingleton<CacheInterceptor>(); // Interceptor'ı ekleyin
        services.AddSingleton(sp =>
        {
            var proxyGenerator = new ProxyGenerator();
            var interceptor = sp.GetRequiredService<CacheInterceptor>();
            var originalCacheService = sp.GetRequiredService<ICacheService>();

            // ICacheService'i proxy ile sar
            return proxyGenerator.CreateInterfaceProxyWithTarget<ICacheService>(originalCacheService, interceptor);
        });

        return services;
    }
}
