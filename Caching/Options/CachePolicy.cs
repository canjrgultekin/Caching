namespace Caching.Options
{
    public class CachePolicy
    {
        public TimeSpan Expiration { get; set; }
        public CachePriority Priority { get; set; } = CachePriority.Normal;
    }

    public enum CachePriority
    {
        Low,
        Normal,
        High,
        Critical
    }
    public enum CacheProviderType
    {
        Redis,
        Hybrid,
        Memory
    }
}
