namespace CacheManager.FileCaching.UnitTests
{
    using CacheManager.Core;
    using CacheManager.Core.Internal;
    using FluentAssertions;
    using System;
    using System.Linq;
    using Xunit;
    using Xunit.Abstractions;

    public class MultipleHandlesTests : IDisposable
    {
        private readonly ICacheManager<string> _cache1;
        private readonly ICacheManager<string> _cache2;
        private readonly Guid _fileCacheName;

        private readonly ITestOutputHelper _output;

        public MultipleHandlesTests(ITestOutputHelper output)
        {
            _output = output;
            _fileCacheName = Guid.NewGuid();

            var config = new ConfigurationBuilder()
#if NETCORE
                .WithJsonSerializer()
#endif
                .WithSystemRuntimeCacheHandle()
                .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(2))
                .EnablePerformanceCounters()
                .EnableStatistics()
                .And
#if NETCORE
                .WithJsonSerializer()
#endif
                .WithFileCacheHandle(_fileCacheName.ToString())
                .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromDays(1))
                .EnablePerformanceCounters()
                .EnableStatistics()
                .Build();

            _cache1 = new BaseCacheManager<string>(config);
            _cache2 = new BaseCacheManager<string>(config);
        }

        public void Dispose()
        {
            _cache1.Clear();
            _cache2.Clear();
        }

        [Fact]
        public void Should_fallback_to_filecache()
        {
            // Arrange
            var myTextKey = "MyKey";
            var myTextValue = "MyValue";

            // First create the value in the FileCacheHandle Separately
            _cache1.Add(myTextKey, myTextValue);

            // Now use the handle, and it should get a miss on the InMemory, but a Hit on the FileCache on disk
            var resultValue = _cache2.Get<string>(myTextKey); // 1x Miss on the in memory, and 1x Hit on File Cache, +1 Get in memory, and +1 get file cache
            var resultValue2 = _cache2.Get<string>(myTextKey); // 1x Hit on in memory, +1 get in memory
            System.Threading.Thread.Sleep(2000); // Sleep 2 milliseconds, in memory should expire
            var resultValue3 = _cache2.Get<string>(myTextKey); // +1 miss on in memory, +1 get memory, +1 remove memory, +1 Hit file cache

            resultValue.Should().Be(myTextValue);
            resultValue2.Should().Be(myTextValue);
            resultValue3.Should().Be(myTextValue);
            _cache1.CacheHandles.Skip(1).First().Stats.GetStatistic(CacheStatsCounterType.AddCalls).Should().Be(1);
            _cache2.CacheHandles.First().Stats.GetStatistic(CacheStatsCounterType.Hits).Should().Be(1);
            _cache2.CacheHandles.First().Stats.GetStatistic(CacheStatsCounterType.Misses).Should().Be(2);
            _cache2.CacheHandles.First().Stats.GetStatistic(CacheStatsCounterType.RemoveCalls).Should().Be(1);
            _cache2.CacheHandles.First().Stats.GetStatistic(CacheStatsCounterType.GetCalls).Should().Be(3);
            _cache2.CacheHandles.Skip(1).First().Stats.GetStatistic(CacheStatsCounterType.Hits).Should().Be(2);
            _cache2.CacheHandles.Skip(1).First().Stats.GetStatistic(CacheStatsCounterType.GetCalls).Should().Be(2);

            foreach (var handle in _cache1.CacheHandles)
            {
                _output.WriteLine("Cache1 = " + GetAllStats(handle.Stats));
            }

            foreach (var handle in _cache2.CacheHandles)
            {
                _output.WriteLine("Cache2 = " + GetAllStats(handle.Stats));
            }
        }

        private string GetAllStats(CacheStats<string> stats)
        {
            return string.Format(
                        "Items: {0}, Hits: {1}, Miss: {2}, Remove: {3}, ClearRegion: {4}, Clear: {5}, Adds: {6}, Puts: {7}, Gets: {8}",
                            stats.GetStatistic(CacheStatsCounterType.Items),
                            stats.GetStatistic(CacheStatsCounterType.Hits),
                            stats.GetStatistic(CacheStatsCounterType.Misses),
                            stats.GetStatistic(CacheStatsCounterType.RemoveCalls),
                            stats.GetStatistic(CacheStatsCounterType.ClearRegionCalls),
                            stats.GetStatistic(CacheStatsCounterType.ClearCalls),
                            stats.GetStatistic(CacheStatsCounterType.AddCalls),
                            stats.GetStatistic(CacheStatsCounterType.PutCalls),
                            stats.GetStatistic(CacheStatsCounterType.GetCalls)
                        );
        }
    }
}
