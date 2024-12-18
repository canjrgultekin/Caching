using Caching.Interfaces;
using Caching.Options;
using Caching.Providers;
using Caching.Security;
using Caching.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using Castle.DynamicProxy;
using Caching.Aspects;
using Caching.Metrics;

namespace Caching.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddAdvancedCaching(
        this IServiceCollection services,
        Action<CacheOptions> configureOptions,
        CacheProviderType providerType,
        bool useEncryption = false,
        byte[] encryptionKey = null,
        byte[] encryptionIv = null)
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

        if (useEncryption)
        {
            if (encryptionKey == null || encryptionIv == null)
                throw new ArgumentException("Encryption key and IV must be provided if encryption is enabled.");

            services.AddSingleton(new AesEncryptionService(encryptionKey, encryptionIv));
        }

        services.AddSingleton<CacheInterceptor>(); // Interceptor
        services.AddSingleton<IProxyGenerator, ProxyGenerator>();

        switch (providerType)
        {
            case CacheProviderType.Memory:
                services.AddMemoryCache();
                services.AddSingleton<ICacheService>(sp =>
                {
                    var memoryCache = sp.GetRequiredService<IMemoryCache>();
                    var encryptionService = useEncryption ? sp.GetRequiredService<AesEncryptionService>() : null;
                    var service = new MemoryCacheService(memoryCache, options, meterProvider, encryptionService);
                    return CreateProxy(sp, service);
                });
                break;

            case CacheProviderType.Redis:
                // RedisCacheService doğrudan kaydediliyor
                services.AddSingleton<RedisCacheService>(sp =>
                {
                    var encryptionService = useEncryption ? sp.GetRequiredService<AesEncryptionService>() : null;
                    return new RedisCacheService(options, sp.GetRequiredService<ISerializer>(), meterProvider, encryptionService);
                });

                services.AddSingleton<ICacheService>(sp =>
                {
                    var redisCacheService = sp.GetRequiredService<RedisCacheService>();
                    return CreateProxy(sp, redisCacheService);
                });
                break;

            case CacheProviderType.Hybrid:
                services.AddMemoryCache();
                services.AddSingleton<ICacheService>(sp =>
                {
                    var memoryCache = sp.GetRequiredService<IMemoryCache>();
                    var distributedCache = sp.GetRequiredService<RedisCacheService>();
                    var encryptionService = useEncryption ? sp.GetRequiredService<AesEncryptionService>() : null;
                    var service = new HybridCacheService(memoryCache, distributedCache, options, meterProvider, encryptionService);
                    return CreateProxy(sp, service);
                });

                // RedisCacheService Hybrid için doğrudan kaydediliyor
                services.AddSingleton<RedisCacheService>(sp =>
                {
                    var encryptionService = useEncryption ? sp.GetRequiredService<AesEncryptionService>() : null;
                    return new RedisCacheService(options, sp.GetRequiredService<ISerializer>(), meterProvider, encryptionService);
                });
                break;
        }

        return services;
    }

    private static ICacheService CreateProxy(IServiceProvider sp, ICacheService service)
    {
        var proxyGenerator = sp.GetRequiredService<IProxyGenerator>();
        var interceptor = sp.GetRequiredService<CacheInterceptor>();
        return proxyGenerator.CreateInterfaceProxyWithTarget(service, interceptor);
    }
}
