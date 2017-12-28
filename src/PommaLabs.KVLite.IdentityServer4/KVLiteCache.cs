using NodaTime;
using PommaLabs.KVLite.Resources;
using System;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.IdentityServer4
{
    /// <summary>
    ///   KVLite-based cache for IdentityServer4.
    /// </summary>
    /// <typeparam name="T">The object type which will be cached.</typeparam>
    public sealed class KVLiteCache<T> : global::IdentityServer4.Services.ICache<T>
        where T : class
    {
        /// <summary>
        ///   Used when receiving null, empty or blank keys.
        /// </summary>
        private const string NoKey = "__none__";

        /// <summary>
        ///   Backing KVLite cache.
        /// </summary>
        private readonly IAsyncCache _cache;

        /// <summary>
        ///   Initializes cache provider.
        /// </summary>
        /// <param name="cache">Backing KVLite cache.</param>
        public KVLiteCache(IAsyncCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(ErrorMessages.NullCache);
        }

        /// <summary>
        ///   The partition used by this cache. Defaults to "IdentityServer4".
        /// </summary>
        public string Partition { get; set; } = nameof(IdentityServer4);

        /// <summary>
        ///   Gets the cached data based upon a key index.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The cached item, or <c>null</c> if no item matches the key.</returns>
        public async Task<T> GetAsync(string key)
        {
            key = string.IsNullOrWhiteSpace(key) ? NoKey : key;
            return (await _cache.GetAsync<T>(Partition, key)).ValueOrDefault();
        }

        /// <summary>
        ///   Caches the data based upon a key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expiration">The expiration.</param>
        /// <returns>A task.</returns>
        public async Task SetAsync(string key, T item, TimeSpan expiration)
        {
            key = string.IsNullOrWhiteSpace(key) ? NoKey : key;
            var lifetime = Duration.FromTimeSpan(expiration);
            await _cache.AddTimedAsync(Partition, key, item, lifetime);
        }
    }
}
