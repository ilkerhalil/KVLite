// File name: ErrorMessages.cs
//
// Author(s): Alessio Parma <alessio.parmagmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parmagmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace PommaLabs.KVLite.Resources
{
    /// <summary>
    ///   Error messages used inside KVLite.
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>
        ///   An error message.
        /// </summary>
        public const string CacheDoesNotAllowPeeking = "{0} does not allow peeking items, therefore this method is not implemented.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string CacheDoesNotAllowSlidingAndAbsolute = "KVLite caching interfaces do not allow setting a sliding item with absolute expiration";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string CacheHasBeenDisposed = "{0} instance has been disposed, therefore no more operations are allowed on it.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string EmptyCacheResult = "Cache result has no value.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string HashMismatch = "Hashes do not match! Expected {0}, found {1}.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string HashNotFound = "Hash not found inside cache value.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnClearAll = "An error occurred while clearing all {Cache} partitions.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnClearPartition = "An error occurred while clearing {Cache} partition '{Partition}'.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnCountAll = "An error occurred while counting items in all {Cache} partitions.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnCountPartition = "An error occurred while counting items in {Cache} partition '{Partition}'.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnDeserialization = "Item '{Partition}/{Key}' from {Cache} could not be deserialized.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnRead = "An error occurred while reading item '{Partition}/{Key}' from {Cache}.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnReadAll = "An error occurred while reading items in all {Cache} partitions.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnReadPartition = "An error occurred while reading items in {Cache} partition '{Partition}'.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnSerialization = "Value '{Value}' could not be serialized.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnVacuum = "An error occurred while applying VACUUM on the SQLite cache.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InternalErrorOnWrite = "An error occurred while writing item '{Partition}/{Key}' into {Cache}.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InvalidCacheEntriesTableName = "Specified name for SQL entries table is not valid.";

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
        public const string InvalidCacheSchemaName = "Specified SQL schema name is not valid.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InvalidCompressionLevel = "Specified compression level is not valid.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string InvalidDataType = "Value data type is not valid.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NotSerializableValue = "Only serializable objects can be stored into the cache. Try putting the [Serializable] attribute on your class, if possible.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullCache = "Cache cannot be null, please specify one valid cache or use either PersistentCache or VolatileCache default instances.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullCacheResolver = "Cache resolver function cannot be null, please specify one non-null function.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullKey = "Key cannot be null, please specify one non-null string.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullOrEmptyCacheFile = "Cache file cannot be null or empty.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullOrEmptyCacheName = "Cache name cannot be null or empty.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullOrEmptyDefaultPartition = "Default partition cannot be null or empty.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullPartition = "Partition cannot be null, please specify one non-null string.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullSettings = "Settings cannot be null, please specify valid settings or use the default instance.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullValue = "Value cannot be null, please specify one non-null object.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string NullValueGetter = "Value getter function cannot be null, please specify one non-null function.";

        /// <summary>
        ///   An error message.
        /// </summary>
        public const string TooManyParentKeys = "Too many parent keys have been specified for this item.";
    }
}
