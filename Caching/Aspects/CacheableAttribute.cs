using System;

namespace Caching.Aspects
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class CacheableAttribute : Attribute
    {
        public string Key { get; }
        public CacheableAttribute(string key)
        {
            Key = key;
        }
    }
}
