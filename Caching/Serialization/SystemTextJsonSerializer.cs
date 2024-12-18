namespace Caching.Serialization;

using Caching.Serialization;
using System.Text.Json;

public class SystemTextJsonSerializer : ISerializer
{
    private readonly JsonSerializerOptions _options;

    public SystemTextJsonSerializer(JsonSerializerOptions options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    public T Deserialize<T>(string data)
    {
        return JsonSerializer.Deserialize<T>(data, _options);
    }
}
