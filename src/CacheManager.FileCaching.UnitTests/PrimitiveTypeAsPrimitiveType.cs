namespace CacheManager.FileCaching.UnitTests
{
    using CacheManager.Core;
    using FluentAssertions;
    using System;
    using Xunit;

    public class PrimitiveTypeAsPrimitiveType : IDisposable
    {
        ICacheManager<string> _cache;

        public PrimitiveTypeAsPrimitiveType()
        {
            var config = new ConfigurationBuilder()
#if NETCORE
                .WithJsonSerializer()
#endif
                .WithFileCacheHandle(Guid.NewGuid().ToString())
                .Build();

            _cache = new BaseCacheManager<string>(config);
        }

        public void Dispose()
        {
            _cache.Clear();
        }

        [Fact]
        public void Should_pass_when_regular_object()
        {
            var key = "something";
            var value = "something else";
            _cache.Add(key, value);

            var resultValue = _cache.Get<string>(key);

            resultValue.Should().Be(value);
        }
    }
}
