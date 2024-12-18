namespace Caching.Options;

public class CacheOptions
{
    public string ConnectionString { get; set; }
    public bool MetricsEnabled { get; set; } = false; // Varsayılan: Kapalı

}
