namespace CacheManager.Core
{
    using CacheManager.FileCaching;
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Extensions for the configuration builder specific to System.Runtime.Caching.FileCache cache handle.
    /// </summary>
    public static class FileCachingBuilderExtensions
    {
        private const string DefaultCacheName = "defaultFileCache";

        /// <summary>
        /// Adds a <see cref="FileCacheHandle{TCacheValue}" /> using a <see cref="System.Runtime.Caching.FileCache"/>.
        /// The name of the cache instance will be 'default'.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="isBackplaneSource">Set this to true if this cache handle should be the source of the backplane.
        /// This setting will be ignored if no backplane is configured.</param>
        /// <returns>
        /// The builder part.
        /// </returns>
        /// <returns>The builder part.</returns>
        public static ConfigurationBuilderCacheHandlePart WithFileCacheHandle(this ConfigurationBuilderCachePart part, bool calculateCacheSize = false, TimeSpan cleanInterval = new TimeSpan(), bool isBackplaneSource = false)
            => part?.WithHandle(typeof(FileCacheHandle<>), DefaultCacheName, isBackplaneSource, new FileCacheHandleAdditionalConfiguration { CalculateCacheSize = calculateCacheSize, CleanInterval = cleanInterval });

        /// <summary>
        /// Adds a <see cref="FileCacheHandle{TCacheValue}" /> using a <see cref="System.Runtime.Caching.FileCache"/> instance with the given <paramref name="instanceName"/>.
        /// The named cache instance can be configured via <c>app/web.config</c> <c>system.runtime.caching</c> section.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="instanceName">The name to be used for the cache instance.</param>
        /// <param name="isBackplaneSource">Set this to true if this cache handle should be the source of the backplane.
        /// This setting will be ignored if no backplane is configured.</param>
        /// <returns>
        /// The builder part.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">If part is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instanceName"/> is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithFileCacheHandle(this ConfigurationBuilderCachePart part, string cacheRoot, bool calculateCacheSize = false, TimeSpan cleanInterval = new TimeSpan(), bool isBackplaneSource = false)
            => part?.WithHandle(typeof(FileCacheHandle<>), cacheRoot, isBackplaneSource, new FileCacheHandleAdditionalConfiguration { CalculateCacheSize = calculateCacheSize, CleanInterval = cleanInterval });

        public static ConfigurationBuilderCacheHandlePart WithFileCacheHandle(this ConfigurationBuilderCachePart part, string cacheRoot, SerializationBinder binder, bool calculateCacheSize = false, TimeSpan cleanInterval = new TimeSpan(), bool isBackplaneSource = false)
            => part?.WithHandle(typeof(FileCacheHandle<>), cacheRoot, isBackplaneSource, new FileCacheHandleAdditionalConfiguration { SerializationBinder = binder, CalculateCacheSize = calculateCacheSize, CleanInterval = cleanInterval });

        public static ConfigurationBuilderCacheHandlePart WithFileCacheHandle(this ConfigurationBuilderCachePart part, SerializationBinder binder, bool calculateCacheSize = false, TimeSpan cleanInterval = new TimeSpan(), bool isBackplaneSource = false)
            => part?.WithHandle(typeof(FileCacheHandle<>), DefaultCacheName, isBackplaneSource, new FileCacheHandleAdditionalConfiguration { SerializationBinder = binder, CalculateCacheSize = calculateCacheSize, CleanInterval = cleanInterval });
    }
}
