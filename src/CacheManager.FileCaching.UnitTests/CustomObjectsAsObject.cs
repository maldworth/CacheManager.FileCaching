namespace CacheManager.FileCaching.UnitTests
{
    using CacheManager.Core;
    using FluentAssertions;
    using System;
    using Xunit;

    public class CustomObjectsAsObject : IDisposable
    {
        ICacheManager<object> _cache;

        public CustomObjectsAsObject()
        {
            var config = new ConfigurationBuilder()
                .WithJsonSerializer()
                .WithFileCacheHandle(Guid.NewGuid().ToString())
                .Build();

            _cache = new BaseCacheManager<object>(config);
        }

        public void Dispose()
        {
            _cache.Clear();
        }

        [Fact]
        public void Should_deserialize_with_cacheitem_toobject()
        {
            var obj = new CustomObject
            {
                FirstProperty = "Hello",
                SecondProperty = "World"
            };

            _cache.Add(nameof(obj), obj);

            var theObj = _cache.GetCacheItem(nameof(obj)).ToObject<CustomObject>();

            theObj.Should().BeEquivalentTo(obj);
        }

        [Fact]
        public void Should_deserialize_with_icache_toobject()
        {
            var obj = new CustomObject
            {
                FirstProperty = "Hello",
                SecondProperty = "World"
            };

            _cache.Add(nameof(obj), obj);

            var theObj = _cache.ToObject<CustomObject>(nameof(obj));

            theObj.Should().BeEquivalentTo(obj);
        }

        [Fact]
        public void Should_fail_cast_with_json_serialization()
        {
            var obj = new CustomObject
            {
                FirstProperty = "Hello",
                SecondProperty = "World"
            };

            _cache.Add(nameof(obj), obj);

            Action a = () => _cache.Get<CustomObject>(nameof(obj));

            // Using Json Serialization returns a JToken, so we cannot cast that into our Custom Object
            a.Should().Throw<InvalidCastException>();
        }
    }
}
