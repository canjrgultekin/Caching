using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Caching.Extensions;
using CachingTestProject;
using Caching.Options;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        var cacheOptions = context.Configuration.GetSection("CacheOptions");

        Console.WriteLine("===== STARTING CACHE PROVIDER TESTS =====");

        // Memory Cache Testi
        Console.WriteLine("\n--- Testing Memory Cache ---");
        ConfigureMemoryCache(services, cacheOptions);
        var memoryProvider = services.BuildServiceProvider();
        TestProductService(memoryProvider, "Caching.MemoryCache").Wait();

        // Redis Cache Testi
        Console.WriteLine("\n--- Testing Redis Cache ---");
        ConfigureRedisCache(services, cacheOptions);
        var redisProvider = services.BuildServiceProvider();
        TestProductService(redisProvider, "Caching.RedisCache").Wait();

        // Hybrid Cache Testi
        Console.WriteLine("\n--- Testing Hybrid Cache ---");
        ConfigureHybridCache(services, cacheOptions);
        var hybridProvider = services.BuildServiceProvider();
        TestProductService(hybridProvider, "Caching.HybridCache").Wait();
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
void ConfigureMemoryCache(IServiceCollection services, IConfigurationSection cacheOptions)
{
    services.AddAdvancedCaching(options =>
    {
        options.ConnectionString = cacheOptions["ConnectionString"];
        options.MetricsEnabled = bool.Parse(cacheOptions["MetricsEnabled"]);
    }, CacheProviderType.Memory);

    services.AddTransient<ProductService>(); // ProductService kaydı
}

// Metot: Redis Cache'i Yapılandır
void ConfigureRedisCache(IServiceCollection services, IConfigurationSection cacheOptions)
{
    services.AddAdvancedCaching(options =>
    {
        options.ConnectionString = cacheOptions["ConnectionString"];
        options.MetricsEnabled = bool.Parse(cacheOptions["MetricsEnabled"]);
    }, CacheProviderType.Redis);

    services.AddTransient<ProductService>(); // ProductService kaydı
}

// Metot: Hybrid Cache'i Yapılandır
void ConfigureHybridCache(IServiceCollection services, IConfigurationSection cacheOptions)
{
    services.AddAdvancedCaching(options =>
    {
        options.ConnectionString = cacheOptions["ConnectionString"];
        options.MetricsEnabled = bool.Parse(cacheOptions["MetricsEnabled"]);
    }, CacheProviderType.Hybrid);

    services.AddTransient<ProductService>(); // ProductService kaydı
}
