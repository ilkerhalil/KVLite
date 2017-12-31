// File name: ErrorMessages.cs
//
// Author(s): Alessio Parma <alessio.parmagmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2018 Alessio Parma <alessio.parmagmail.com>
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
        public static string CacheDoesNotAllowPeeking { get; } = "{0} does not allow peeking items, therefore this method is not implemented";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string CacheDoesNotAllowSlidingAndAbsolute { get; } = "KVLite caching interfaces do not allow setting a sliding item with absolute expiration";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string CacheHasBeenDisposed { get; } = "{0} instance has been disposed, therefore no more operations are allowed on it";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string EmptyCacheResult { get; } = "Cache result has no value";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string HashMismatch { get; } = "Hashes do not match! Expected {0}, found {1}";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string HashNotFound { get; } = "Hash not found inside cache value";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InternalErrorOnClearAll { get; } = "An error occurred while clearing all {Cache} partitions";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InternalErrorOnClearPartition { get; } = "An error occurred while clearing {Cache} partition '{Partition}'";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InternalErrorOnCountAll { get; } = "An error occurred while counting items in all {Cache} partitions";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InternalErrorOnCountPartition { get; } = "An error occurred while counting items in {Cache} partition '{Partition}'";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InternalErrorOnDeserialization { get; } = "Item '{Partition}/{Key}' from {Cache} could not be deserialized";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InternalErrorOnRead { get; } = "An error occurred while reading item '{Partition}/{Key}' from {Cache}";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InternalErrorOnReadAll { get; } = "An error occurred while reading items in all {Cache} partitions";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InternalErrorOnReadPartition { get; } = "An error occurred while reading items in {Cache} partition '{Partition}'";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InternalErrorOnSerialization { get; } = "Value '{Value}' could not be serialized";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InternalErrorOnVacuum { get; } = "An error occurred while applying VACUUM on the SQLite cache";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InternalErrorOnWrite { get; } = "An error occurred while writing item '{Partition}/{Key}' into {Cache}";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InvalidCacheEntriesTableName { get; } = "Specified name for SQL entries table is not valid";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InvalidCacheName { get; } = "In-memory cache name can only contain alphanumeric characters, dots and underscores";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InvalidCacheReadMode { get; } = "An invalid enum value was given for cache read mode";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InvalidCacheSchemaName { get; } = "Specified SQL schema name is not valid";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InvalidCompressionLevel { get; } = "Specified compression level is not valid";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string InvalidDataType { get; } = "Value data type is not valid";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string NotSerializableValue { get; } = "Only serializable objects can be stored into the cache. Try putting the [Serializable] attribute on your class, if possible";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string NullCache { get; } = "Cache cannot be null, please specify one valid cache or use either PersistentCache or VolatileCache default instances";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string NullCacheResolver { get; } = "Cache resolver function cannot be null, please specify one non-null function";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string NullKey { get; } = "Key cannot be null, please specify one non-null string";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string NullOrEmptyCacheFile { get; } = "Cache file cannot be null or empty";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string NullOrEmptyCacheName { get; } = "Cache name cannot be null or empty";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string NullOrEmptyDefaultPartition { get; } = "Default partition cannot be null or empty";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string NullPartition { get; } = "Partition cannot be null, please specify one non-null string";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string NullSettings { get; } = "Settings cannot be null, please specify valid settings or use the default instance";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string NullValue { get; } = "Value cannot be null, please specify one non-null object";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string NullValueGetter { get; } = "Value getter function cannot be null, please specify one non-null function";

        /// <summary>
        ///   An error message.
        /// </summary>
        public static string TooManyParentKeys { get; } = "Too many parent keys have been specified for this item";
    }
}
