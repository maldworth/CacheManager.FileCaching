namespace CacheManager.FileCaching
{
    using CacheManager.Core;
    using CacheManager.Core.Internal;
    using CacheManager.Core.Logging;
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Runtime.Caching;
    using System.Runtime.Serialization;
    using static CacheManager.Core.Utility.Guard;

    /// <summary>
    /// Definition for additional FileCache configurations
    /// </summary>
    public class FileCacheHandleAdditionalConfiguration
    {
        ///// <summary>
        ///// Gets or sets the cache root
        ///// </summary>
        ///// <value>
        ///// The cache's root file path.
        ///// </value>
        //public string CacheRoot { get; set; }

        /// <summary>
        /// Gets or sets the serialization binder.
        /// </summary>
        /// <value>
        /// The serialization binder enables caching of custom objects.
        /// </value>
        public SerializationBinder SerializationBinder { get; set; }

        /// <summary>
        /// Gets or sets calculate cache size.
        /// </summary>
        /// <value>
        /// A flag indicating whether or not to calculate the cache size on initialization
        /// </value>
        public bool CalculateCacheSize { get; set; }

        /// <summary>
        /// Gets or sets clean interval.
        /// </summary>
        /// <value>
        /// If supplied, sets the interval of time that must occur between self cleans
        /// </value>
        public TimeSpan CleanInterval { get; set; } = new TimeSpan();
    }

    /// <summary>
    /// Simple implementation for a json based file cache. Limitations are blocking on writes to the file
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    /// <remarks>
    /// Although the FileCache doesn't support regions nor a RemoveAll/Clear method, we will
    /// implement it via cache dependencies.
    /// </remarks>
    public class FileCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private const string DefaultCacheName = "defaultFileCache";

        // can be default or any other name
        private readonly string _cacheName;

        private volatile FileCache _cache = null;
        
        private ICacheSerializer _serializer;

        /// <summary>
        /// Gets the underlying fileCache used by this FileCacheHandle implementation
        /// 
        /// </summary>
        public FileCache FileCache => _cache;

        /// <summary>
        /// Gets the cache settings.
        /// </summary>
        /// <value>The cache settings.</value>
        public NameValueCollection CacheSettings => GetSettings(_cache);

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public override int Count => (int)_cache.GetCount();

        /// <inheritdoc />
        protected override ILogger Logger { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="additionalConfiguration">The FileCache additional configuration.</param>
        public FileCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory, ICacheSerializer serializer, FileCacheHandleAdditionalConfiguration additionalConfiguration)
            : this(managerConfiguration, configuration, loggerFactory, additionalConfiguration)
        {
            NotNull(serializer, nameof(serializer));
            _serializer = serializer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="additionalConfiguration">The FileCache additional configuration.</param>
        public FileCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory, FileCacheHandleAdditionalConfiguration additionalConfiguration)
            : base(managerConfiguration, configuration)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));

            Logger = loggerFactory.CreateLogger(this);
            _cacheName = configuration.Name ?? DefaultCacheName;

            if (additionalConfiguration == null)
                additionalConfiguration = new FileCacheHandleAdditionalConfiguration();

            if(additionalConfiguration.SerializationBinder == null)
                _cache = new FileCache(_cacheName, additionalConfiguration.CalculateCacheSize, additionalConfiguration.CleanInterval);
            else
                _cache = new FileCache(_cacheName, additionalConfiguration.SerializationBinder, additionalConfiguration.CalculateCacheSize, additionalConfiguration.CleanInterval);
        }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            _cache.Flush();
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        public override void ClearRegion(string region)
        {
            _cache.Flush(region);
        }

        /// <inheritdoc />
        public override bool Exists(string key)
        {
            return _cache.Contains(key);
        }

        /// <inheritdoc />
        public override bool Exists(string key, string region)
        {
            return _cache.Contains(key, region);
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            if (_cache.Contains(item.Key, item.Region))
            {
                return false;
            }

            var policy = GetPolicy(item);

            if (_serializer != null)
            {
                return _cache.Add(item.Key, _serializer.SerializeCacheItem(item), policy, item.Region);
            }
            else
                return _cache.Add(item.Key, item, policy, item.Region);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key) => GetCacheItemInternal(key, null);

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            CacheItem<TCacheValue> item = null;

            if (_serializer != null)
            {
                // If it's expired, this result could be null, so we just set Item to null
                var cacheItemBytes = _cache.Get(key, region);

                if (cacheItemBytes != null)
                    item = _serializer.DeserializeCacheItem<TCacheValue>((byte[])cacheItemBytes, typeof(TCacheValue));
            }
            else
                item = (CacheItem<TCacheValue>)_cache.Get(key, region);

            if (item == null)
            {
                return null;
            }

            // maybe the item is already expired because FileCache implements a default interval
            // of 20 seconds! to check for expired items on each store, we do it on access to also
            // reflect smaller time frames especially for sliding expiration...
            // cache.Get eventually triggers eviction callback, but just in case...
            if (item.IsExpired)
            {
                RemoveInternal(item.Key, item.Region);
                TriggerCacheSpecificRemove(item.Key, item.Region, CacheItemRemovedReason.Expired, item.Value);
                return null;
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                // because we don't use UpdateCallback because of some multithreading issues lets
                // try to simply reset the item by setting it again.
                // item = this.GetItemExpiration(item); // done via base cache handle

                if (_serializer != null)
                    _cache.Set(key, _serializer.SerializeCacheItem(item), GetPolicy(item), region);
                else
                    _cache.Set(key, item, GetPolicy(item), region);
            }

            return item;
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            var policy = GetPolicy(item);

            if (_serializer != null)
                _cache.Set(item.Key, _serializer.SerializeCacheItem(item), policy, item.Region);
            else
                _cache.Set(item.Key, item, policy, item.Region);
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key) => RemoveInternal(key, null);

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key, string region)
        {
            var obj = _cache.Remove(key, region);

            return obj != null;
        }

        private static NameValueCollection GetSettings(FileCache instance)
        {
            var cacheCfg = new NameValueCollection
            {
                { "CacheMemoryLimitMegabytes", (instance.MaxCacheSize / 1024 / 1024).ToString(CultureInfo.InvariantCulture) },
                { "PhysicalMemoryLimitPercentage", "Need to scan drive folder is on and display free size" }
                //{ "PollingInterval", instance.PollingInterval.ToString() }
            };

            return cacheCfg;
        }

        private CacheItemPolicy GetPolicy(CacheItem<TCacheValue> item)
        {
            var policy = new CacheItemPolicy()
            {
                Priority = CacheItemPriority.Default,
                // We don't use the Change Monitors because we don't need to clear out our cache based on a dependency (of the instance key)
                // We create the ClearCache methods manually to go and delete all files in the folders.
                AbsoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration,
                SlidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration,
            };

            if (item.ExpirationMode == ExpirationMode.Absolute)
            {
                policy.AbsoluteExpiration = new DateTimeOffset(DateTime.UtcNow.Add(item.ExpirationTimeout));
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                policy.SlidingExpiration = item.ExpirationTimeout;
            }

            item.LastAccessedUtc = DateTime.UtcNow;

            return policy;
        }
    }
}
