using Caching.Interfaces;

namespace CachingTestProject;

public class ProductServiceWithoutAspect
{
    private readonly ICacheService _cacheService;

    public ProductServiceWithoutAspect(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<Product> GetProductAsync(int productId)
    {
        string cacheKey = $"Product_{productId}";

        // Cache'den veri al
        var cachedProduct = await _cacheService.GetAsync<Product>(cacheKey);
        if (cachedProduct != null)
        {
            Console.WriteLine($"[GetProductAsync] Cache hit for key: {cacheKey}");
            return cachedProduct;
        }

        // Cache'de yoksa veritabanından al
        Console.WriteLine($"[GetProductAsync] Cache miss for key: {cacheKey}. Fetching from database...");
        var product = new Product { Id = productId, Name = $"Product {productId}" };

        // Cache'e ekle
        await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(5));
        Console.WriteLine($"[GetProductAsync] Added product to cache with key: {cacheKey}");

        return product;
    }

    public async Task RemoveProductFromCacheAsync(int productId)
    {
        string cacheKey = $"Product_{productId}";

        // Cache'den sil
        var removed = await _cacheService.RemoveAsync(cacheKey);
        Console.WriteLine(removed
            ? $"[RemoveProductFromCacheAsync] Removed product with key: {cacheKey} from cache."
            : $"[RemoveProductFromCacheAsync] Key: {cacheKey} not found in cache.");
    }
}


