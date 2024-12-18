using Castle.DynamicProxy;
using Caching.Interfaces;

namespace Caching.Aspects
{
    public class CacheInterceptor : IInterceptor
    {
        private readonly ICacheService _cacheService;

        public CacheInterceptor(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public void Intercept(IInvocation invocation)
        {
            var cacheableAttribute = invocation.Method.GetCustomAttributes(typeof(CacheableAttribute), true)
                .FirstOrDefault() as CacheableAttribute;

            if (cacheableAttribute != null)
            {
                // Cache Key'i oluştur
                var cacheKey = GenerateCacheKey(cacheableAttribute.Key, invocation.Arguments);

                // Cache'den kontrol et
                var returnType = invocation.Method.ReturnType.GetGenericArguments()[0];
                var cachedValueTask = (Task<object>)typeof(ICacheService).GetMethod(nameof(ICacheService.GetAsync))
                    ?.MakeGenericMethod(returnType)
                    .Invoke(_cacheService, new object[] { cacheKey });

                cachedValueTask?.Wait();
                var cachedValue = cachedValueTask?.Result;

                if (cachedValue != null)
                {
                    Console.WriteLine($"[CacheInterceptor] Cache hit for key: {cacheKey}");
                    invocation.ReturnValue = Task.FromResult(cachedValue);
                    return;
                }

                Console.WriteLine($"[CacheInterceptor] Cache miss for key: {cacheKey}");
            }

            // Orijinal metodu çağır
            invocation.Proceed();

            if (cacheableAttribute != null)
            {
                var cacheKey = GenerateCacheKey(cacheableAttribute.Key, invocation.Arguments);

                // Cache'e kaydet
                if (invocation.ReturnValue is Task task && task.GetType().IsGenericType)
                {
                    var result = task.GetType().GetProperty("Result")?.GetValue(task);
                    if (result != null)
                    {
                        Console.WriteLine($"[CacheInterceptor] Saving result to cache with key: {cacheKey}");
                        _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5)).Wait();
                    }
                }
            }
        }

        private string GenerateCacheKey(string keyTemplate, object[] arguments)
        {
            // Key şablonunu (template) metot parametreleriyle değiştir
            return string.Format(keyTemplate, arguments);
        }
    }
}
