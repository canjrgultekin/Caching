using Caching.Serialization;
using Newtonsoft.Json;

namespace Caching.Serialization;

public class NewtonsoftJsonSerializer : ISerializer
{
    public string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj);
    public T Deserialize<T>(string data) => JsonConvert.DeserializeObject<T>(data);
}
