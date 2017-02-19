// Copyright 2015-2025 Alessio Parma <alessio.parma@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
// in compliance with the License. You may obtain a copy of the License at:
//
// "http://www.apache.org/licenses/LICENSE-2.0"
//
// Unless required by applicable law or agreed to in writing, software distributed under the License
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions and limitations under
// the License.

namespace PommaLabs.CodeServices.Caching.Core
{
    /// <summary>
    ///   Error messages used inside KVLite.
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnClearAll = "An error occurred while clearing all cache partitions.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnClearPartition = "An error occurred while clearing cache partition '{0}'.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnCountAll = "An error occurred while counting items in all cache partitions.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnCountPartition = "An error occurred while counting items in cache partition '{0}'.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnReadAll = "An error occurred while reading items in all cache partitions.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnReadPartition = "An error occurred while reading items in cache partition '{0}'.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnRead = "An error occurred while reading item '{0}/{1}' from the cache.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnWrite = "An error occurred while writing item '{0}/{1}' into the cache.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnVacuum = "An error occurred while applying VACUUM on the SQLite cache.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InvalidCacheName = "In-memory cache name can only contain alphanumeric characters, dots and underscores.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InvalidCacheReadMode = "An invalid enum value was given for cache read mode.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NotSerializableValue = @"Only serializable objects can be stored in the cache. Try putting the [Serializable] attribute on your class, if possible.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullOrEmptyCacheName = @"Cache name cannot be null or empty.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullOrEmptyCacheFile = @"Cache file cannot be null or empty.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullCache = @"Cache cannot be null, please specify one valid cache or use either PersistentCache or VolatileCache default instances.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullCacheResolver = @"Cache resolver function cannot be null, please specify one non-null function.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullKey = @"Key cannot be null, please specify one non-null string.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullPartition = @"Partition cannot be null, please specify one non-null string.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullSettings = @"Settings cannot be null, please specify valid settings or use the default instance.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullValue = @"Value cannot be null, please specify one non-null object.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullValueGetter = @"Value getter function cannot be null, please specify one non-null function.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string CacheHasBeenDisposed = @"Cache instance has been disposed, therefore no more operations are allowed on it.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string CacheDoesNotAllowPeeking = @"This cache does not allow peeking items, therefore this method is not implemented.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string CacheDoesNotAllowSlidingAndAbsolute = @"CodeServices caching interfaces do not allow setting a sliding item with absolute expiration";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string TooManyParentKeys = @"Too many parent keys have been specified for this item.";
    }
}
