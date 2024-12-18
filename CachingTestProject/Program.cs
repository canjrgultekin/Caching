using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Caching.Extensions;
using CachingTestProject;
using Caching.Options;
using System.Text;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        var cacheOptions = context.Configuration.GetSection("CacheOptions");

        Console.WriteLine("===== STARTING CACHE PROVIDER TESTS =====");

        // Şifreleme için anahtar ve IV
        var encryptionKey = Encoding.UTF8.GetBytes("abcdefghijklmnopqrstuvwx123456789012");
        var encryptionIv = Encoding.UTF8.GetBytes("1234567890123456");

        // Memory Cache Testi (Şifrelemesiz)
        Console.WriteLine("\n--- Testing Memory Cache (No Encryption) ---");
        ConfigureMemoryCache(services, cacheOptions, useEncryption: false);
        var memoryProviderNoEnc = services.BuildServiceProvider();
        TestProductService(memoryProviderNoEnc, "Caching.MemoryCache (No Encryption)").Wait();

        // Memory Cache Testi (Şifrelemeli)
        Console.WriteLine("\n--- Testing Memory Cache (With Encryption) ---");
        ConfigureMemoryCache(services, cacheOptions, useEncryption: true, encryptionKey, encryptionIv);
        var memoryProviderEnc = services.BuildServiceProvider();
        TestProductService(memoryProviderEnc, "Caching.MemoryCache (With Encryption)").Wait();

        // Redis Cache Testi (Şifrelemesiz)
        Console.WriteLine("\n--- Testing Redis Cache (No Encryption) ---");
        ConfigureRedisCache(services, cacheOptions, useEncryption: false);
        var redisProviderNoEnc = services.BuildServiceProvider();
        TestProductService(redisProviderNoEnc, "Caching.RedisCache (No Encryption)").Wait();

        // Redis Cache Testi (Şifrelemeli)
        Console.WriteLine("\n--- Testing Redis Cache (With Encryption) ---");
        ConfigureRedisCache(services, cacheOptions, useEncryption: true, encryptionKey, encryptionIv);
        var redisProviderEnc = services.BuildServiceProvider();
        TestProductService(redisProviderEnc, "Caching.RedisCache (With Encryption)").Wait();

        // Hybrid Cache Testi (Şifrelemesiz)
        Console.WriteLine("\n--- Testing Hybrid Cache (No Encryption) ---");
        ConfigureHybridCache(services, cacheOptions, useEncryption: false);
        var hybridProviderNoEnc = services.BuildServiceProvider();
        TestProductService(hybridProviderNoEnc, "Caching.HybridCache (No Encryption)").Wait();

        // Hybrid Cache Testi (Şifrelemeli)
        Console.WriteLine("\n--- Testing Hybrid Cache (With Encryption) ---");
        ConfigureHybridCache(services, cacheOptions, useEncryption: true, encryptionKey, encryptionIv);
        var hybridProviderEnc = services.BuildServiceProvider();
        TestProductService(hybridProviderEnc, "Caching.HybridCache (With Encryption)").Wait();
    });

var app = builder.Build();

Console.WriteLine("\n===== ALL CACHE PROVIDER TESTS COMPLETED =====");

// Metot: ProductService'i Test Et
async Task TestProductService(IServiceProvider serviceProvider, string providerName)
{
    var productService = serviceProvider.GetRequiredService<ProductService>();

    Console.WriteLine($"\n[{providerName}] Fetching product for ID: 1...");
    var product1 = await productService.GetProductAsync(1);
    Console.WriteLine($"[{providerName}] Product: {product1.Name}");

    Console.WriteLine($"\n[{providerName}] Fetching product for ID: 1 again...");
    var product2 = await productService.GetProductAsync(1);
    Console.WriteLine($"[{providerName}] Product: {product2.Name}");
}

// Metot: Memory Cache'i Yapılandır
void ConfigureMemoryCache(IServiceCollection services, IConfigurationSection cacheOptions, bool useEncryption, byte[] encryptionKey = null, byte[] encryptionIv = null)
{
    services.AddAdvancedCaching(options =>
    {
        options.ConnectionString = cacheOptions["ConnectionString"];
        options.MetricsEnabled = bool.Parse(cacheOptions["MetricsEnabled"]);
    }, CacheProviderType.Memory,
    useEncryption: useEncryption,
    encryptionKey: encryptionKey,
    encryptionIv: encryptionIv);

    services.AddTransient<ProductService>(); // ProductService kaydı
}

// Metot: Redis Cache'i Yapılandır
void ConfigureRedisCache(IServiceCollection services, IConfigurationSection cacheOptions, bool useEncryption, byte[] encryptionKey = null, byte[] encryptionIv = null)
{
    services.AddAdvancedCaching(options =>
    {
        options.ConnectionString = cacheOptions["ConnectionString"];
        options.MetricsEnabled = bool.Parse(cacheOptions["MetricsEnabled"]);
    }, CacheProviderType.Redis,
    useEncryption: useEncryption,
    encryptionKey: encryptionKey,
    encryptionIv: encryptionIv);

    services.AddTransient<ProductService>(); // ProductService kaydı
}

// Metot: Hybrid Cache'i Yapılandır
void ConfigureHybridCache(IServiceCollection services, IConfigurationSection cacheOptions, bool useEncryption, byte[] encryptionKey = null, byte[] encryptionIv = null)
{
    services.AddAdvancedCaching(options =>
    {
        options.ConnectionString = cacheOptions["ConnectionString"];
        options.MetricsEnabled = bool.Parse(cacheOptions["MetricsEnabled"]);
    }, CacheProviderType.Hybrid,
    useEncryption: useEncryption,
    encryptionKey: encryptionKey,
    encryptionIv: encryptionIv);

    services.AddTransient<ProductService>(); // ProductService kaydı
}
