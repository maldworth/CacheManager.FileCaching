namespace CacheManager.FileCaching.UnitTests
{
    using CacheManager.Core;
    using FluentAssertions;
    using Newtonsoft.Json.Linq;
    using System;
    using Xunit;

    public class CustomObjectsAsCustomObjects : IDisposable
    {
        ICacheManager<CustomObject> _cache;

        public CustomObjectsAsCustomObjects()
        {
            var config = new ConfigurationBuilder()
                .WithJsonSerializer()
                .WithFileCacheHandle(Guid.NewGuid().ToString())
                .Build();

            _cache = new BaseCacheManager<CustomObject>(config);
        }

        public void Dispose()
        {
            _cache.Clear();
        }

        [Fact]
        public void Should_store_complex_object_with_json_serialization()
        {
            var obj = new CustomObject
            {
                FirstProperty = "Hello",
                SecondProperty = "World"
            };

            _cache.Add(nameof(obj), obj);

            var theObj = _cache.Get<CustomObject>(nameof(obj));

            theObj.Should().BeEquivalentTo(obj);
        }
    }

    public static class CacheItemExtensions
    {
        public static TCacheValue ToObject<TCacheValue>(this CacheItem<object> cacheItem)
        {
            if (cacheItem.Value is JToken jToken)
            {
                return jToken.ToObject<TCacheValue>();
            }
            else
            {
                throw new InvalidCastException("Please use Newtonsoft Json Serializer when using this ExtensionMethod");
            }
        }

        public static TCacheValue ToObject<TCacheValue>(this object cacheValue)
        {
            if (cacheValue is JToken jToken)
            {
                return jToken.ToObject<TCacheValue>();
            }
            else
            {
                throw new InvalidCastException("Please use Newtonsoft Json Serializer when using this ExtensionMethod");
            }
        }

        public static TCacheValue ToObject<TCacheValue>(this ICache<object> cache, string key)
        {
            return cache.GetCacheItem(key).ToObject<TCacheValue>();
        }

        public static TCacheValue ToObject<TCacheValue>(this ICache<object> cache, string key, string region)
        {
            return cache.GetCacheItem(key, region).ToObject<TCacheValue>();
        }
    }
}
