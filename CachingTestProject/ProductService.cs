using Caching.Aspects;

namespace CachingTestProject
{
    public class ProductService
    {
        private static readonly Dictionary<int, Product> DummyDatabase = new Dictionary<int, Product>();

        [Cacheable("GetProduct_{productId}")]
        public async Task<Product> GetProductAsync(int productId)
        {
            if (!DummyDatabase.ContainsKey(productId))
            {
                Console.WriteLine($"[ProductService] Populating dummy database for ID: {productId}");
                // İlk çağrıda dummy data oluşturuluyor
                DummyDatabase[productId] = new Product
                {
                    Id = productId,
                    Name = $"Product {productId}"
                };
            }

            Console.WriteLine($"Fetching Product for ID: {productId}");
            return await Task.FromResult(DummyDatabase[productId]);
        }
    }

}
