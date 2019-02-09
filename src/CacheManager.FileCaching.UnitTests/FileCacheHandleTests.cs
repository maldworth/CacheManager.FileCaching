namespace CacheManager.FileCaching.UnitTests
{
    using CacheManager.Core;
    using FluentAssertions;
    using System;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Xunit;

    public class FileCacheHandleTests : IDisposable
    {
        ICacheManager<object> _cache;

        public FileCacheHandleTests()
        {
            var config = new ConfigurationBuilder()
#if NETCORE
                .WithJsonSerializer()
#endif
                .WithFileCacheHandle(Guid.NewGuid().ToString())
                .Build();

            _cache = new BaseCacheManager<object>(config);
        }

        public void Dispose()
        {
            _cache.Clear();
        }

        [Fact]
        public void Should_clear_cache()
        {
            var myTextKey = "MyDescription";
            var myTextValue = "The interest calculation";

            var myAmountKey = "MyAmount";
            var myAmountValue = 100;

            var myModifierKey = "MyModifier";
            var myModifierValue = 0.15;

            _cache.Add(myTextKey, myTextValue);
            _cache.Add(myAmountKey, myAmountValue);
            _cache.Add(myModifierKey, myModifierValue);

            _cache.Exists(myTextKey).Should().BeTrue();
            _cache.Exists(myAmountKey).Should().BeTrue();
            _cache.Exists(myModifierKey).Should().BeTrue();

            _cache.Clear();

            _cache.Exists(myTextKey).Should().BeFalse();
            _cache.Exists(myAmountKey).Should().BeFalse();
            _cache.Exists(myModifierKey).Should().BeFalse();
        }

        [Fact]
        public void Should_cache_items()
        {
            var myTextKey = "MyDescription";
            var myTextValue = "The interest calculation";

            var myAmountKey = "MyAmount";
            var myAmountValue = 100;

            var myModifierKey = "MyModifier";
            var myModifierValue = 0.15;

            _cache.Add(myTextKey, myTextValue);
            _cache.Add(myAmountKey, myAmountValue);
            _cache.Add(myModifierKey, myModifierValue);

            myTextValue.Should().Be(_cache.Get<string>(myTextKey));
            myAmountValue.Should().Be(_cache.Get<int>(myAmountKey));
            myModifierValue.Should().Be(_cache.Get<double>(myModifierKey));

        }

        [Fact]
        public void Should_clean_expired_cache_items()
        {
            var firstCacheItem = new CacheItem<object>("MyFirstItem", "some value", ExpirationMode.Absolute, TimeSpan.FromSeconds(1));
            var secondCacheItem = new CacheItem<object>("MySecondItem", "some other value", ExpirationMode.Absolute, TimeSpan.FromSeconds(2));

            _cache.Add(firstCacheItem);
            _cache.Add(secondCacheItem);

            System.Threading.Thread.Sleep(1500); // Sleep for 1.5 seconds seconds

            _cache.GetCacheItem(firstCacheItem.Key).Should().BeNull();
            _cache.GetCacheItem(secondCacheItem.Key).Should().NotBeNull();
        }

#if !NETCORE
        // Binary Serializer doesn't work in .NET Standard (Michio of Cache Manager says they neeed to make System.Type serializable for him to be able to do that)
        [Fact]
        public void Should_not_store_complex_object_not_serializable()
        {
            var obj = new CustomObject
            {
                FirstProperty = "Hello",
                SecondProperty = "World"
            };

            Action a = () => _cache.Add(nameof(obj), obj);

            a.Should().Throw<SerializationException>();
        }

        internal class CustomObject
        {
            public string FirstProperty { get; set; }
            public string SecondProperty { get; set; }
        }

        [Fact]
        public void Should_store_complex_object()
        {
            var person = new TestPerson
            {
                FirstName = "John",
                LastName = "Smith",
                DateOfBirth = DateTime.Parse("1990-01-05").Date,
                Identifier = 5345345,
                // Leave SomeOtherId Null
                AnotherOtherId = Guid.NewGuid(),
                Address = new Address
                {
                    StreetName = "Chestnut Drive",
                    StreetNumber = 123
                }
            };

            _cache[nameof(person)] = person;
            _cache["SomeOtherValue"] = true;

            var thePerson = (TestPerson)_cache.Get(nameof(person));
            var theBoolValue = (bool)_cache.Get("SomeOtherValue");

            thePerson.Should().BeEquivalentTo(person);
        }

        [Serializable]
        internal class TestPerson
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public DateTime DateOfBirth { get; set; }
            public int Identifier { get; set; }
            public Guid? SomeOtherId { get; set; }
            public Guid? AnotherOtherId { get; set; }
            public Address Address { get; set; }
        }

        [Serializable]
        internal class Address
        {
            public int StreetNumber { get; set; }
            public string StreetName { get; set; }
        }
#endif
    }
}
