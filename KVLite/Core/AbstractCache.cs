// File name: AbstractCache.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
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

using Common.Logging;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Common;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Abstract class which should make it easier to implement a new kind of cache.
    /// </summary>
    /// <typeparam name="TCacheSettings">The type of the cache settings.</typeparam>
    public abstract class AbstractCache<TCacheSettings> : FormattableObject, ICache<TCacheSettings>
        where TCacheSettings : AbstractCacheSettings
    {
        #region Abstract members

        /// <summary>
        ///   Gets the clock used by the cache.
        /// </summary>
        /// <value>The clock used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="SystemClock"/>.
        /// </remarks>
        public abstract IClock Clock { get; }

        /// <summary>
        ///   Gets the compressor used by the cache.
        /// </summary>
        /// <value>The compressor used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="DeflateCompressor"/>.
        /// </remarks>
        public abstract ICompressor Compressor { get; }

        /// <summary>
        ///   Gets the log used by the cache.
        /// </summary>
        /// <value>The log used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to what <see
        ///   cref="LogManager.GetLogger(System.Type)"/> returns.
        /// </remarks>
        public abstract ILog Log { get; }

        /// <summary>
        ///   The maximum number of parent keys each item can have.
        /// </summary>
        public abstract int MaxParentKeyCountPerItem { get; }

        /// <summary>
        ///   Gets the serializer used by the cache.
        /// </summary>
        /// <value>The serializer used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="JsonSerializer"/>. Therefore,
        ///   if you do not specify another serializer, make sure that your objects are serializable
        ///   (in most cases, simply use the <see cref="SerializableAttribute"/> and expose fields as
        ///   public properties).
        /// </remarks>
        public abstract ISerializer Serializer { get; }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        /// <value>The available settings for the cache.</value>
        public abstract TCacheSettings Settings { get; }

        /// <summary>
        ///   True if the Peek methods are implemented, f-alse otherwise.
        /// </summary>
        public abstract bool CanPeek { get; }

        /// <summary>
        ///   Adds given value with the specified expiry time and refresh internal.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry time.</param>
        /// <param name="interval">The refresh interval.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        protected abstract void AddInternal<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval, IList<string> parentKeys);

        /// <summary>
        ///   Clears this instance or a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items that have been removed.</returns>
        protected abstract long ClearInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate);

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected abstract bool ContainsInternal(string partition, string key);

        /// <summary>
        ///   The number of items in the cache or in a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected abstract long CountInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.ConsiderExpiryDate);

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by the corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        protected abstract Option<TVal> GetInternal<TVal>(string partition, string key);

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        protected abstract Option<CacheItem<TVal>> GetItemInternal<TVal>(string partition, string key);

        /// <summary>
        ///   Gets all cache items or the ones in a partition, if specified. If an item is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All cache items.</returns>
        protected abstract CacheItem<TVal>[] GetItemsInternal<TVal>(string partition);

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        protected abstract Option<TVal> PeekInternal<TVal>(string partition, string key);

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        protected abstract Option<CacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key);

        /// <summary>
        ///   Gets the all values in the cache or in the specified partition, without updating expiry dates.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All values, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        protected abstract CacheItem<TVal>[] PeekItemsInternal<TVal>(string partition);

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        protected abstract void RemoveInternal(string partition, string key);

        #endregion Abstract members

        #region IDisposable members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting
        ///   unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
            {
                // Nothing to do, cache has been disposed.
                return;
            }

            Dispose(true);

            // Mark this class as disposed.
            Disposed = true;

            // Use SupressFinalize in case a subclass of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting
        ///   unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if it is a managed dispose, false otherwise.</param>
        protected abstract void Dispose(bool disposing);

        #endregion IDisposable members

        #region ICache members

        /// <summary>
        ///   Whether this cache has been disposed or not. When a cache has been disposed, no more
        ///   operations are allowed on it.
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        ///   The last error "swallowed" by the cache. All KVLite caches, by definition, try to
        ///   swallow as much exceptions as possible, because a failure in the cache should never
        ///   harm the main application. This is an important rule.
        ///
        ///   This property might be used to expose the last error occurred while processing cache
        ///   items. If no error has occurred, this property will simply be null.
        ///
        ///   Every error is carefully logged using the provided <see
        ///   cref="P:PommaLabs.KVLite.ICache.Log"/>, so no information is lost when the cache
        ///   swallows the exception.
        /// </summary>
        public Exception LastError { get; protected set; }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        /// <value>The available settings for the cache.</value>
        AbstractCacheSettings ICache.Settings => Settings;

        /// <summary>
        ///   Gets the value with the specified partition and key.
        /// </summary>
        /// <value>The value with the specified partition and key.</value>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with the specified partition and key.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        /// <remarks>
        ///   This method, differently from other readers (like <see
        ///   cref="Get{TVal}(string,string)"/> or <see cref="Peek{TVal}(string,string)"/>), does not
        ///   have a typed return object, because indexers cannot be generic. Therefore, we have to
        ///   return a simple <see cref="object"/>.
        /// </remarks>
        public Option<object> this[string partition, string key]
        {
            get
            {
                // Preconditions
                RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
                RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
                RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

                try
                {
                    var result = GetInternal<object>(partition, key);

                    // Postconditions
                    Debug.Assert(Contains(partition, key) == result.HasValue);
                    return result;
                }
                catch (Exception ex)
                {
                    LastError = ex;
                    Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                    return Option.None<object>();
                }
            }
        }

        /// <summary>
        ///   Gets the value with the default partition and specified key.
        /// </summary>
        /// <value>The value with the default partition and specified key.</value>
        /// <param name="key">The key.</param>
        /// <returns>The value with the default partition and specified key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <remarks>
        ///   This method, differently from other readers (like <see
        ///   cref="GetFromDefaultPartition{TVal}(string)"/> or <see
        ///   cref="PeekIntoDefaultPartition{TVal}(string)"/>), does not have a typed return object,
        ///   because indexers cannot be generic. Therefore, we have to return a simple <see cref="object"/>.
        /// </remarks>
        public Option<object> this[string key]
        {
            get
            {
                // Preconditions
                RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
                RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

                try
                {
                    var result = GetInternal<object>(Settings.DefaultPartition, key);

                    // Postconditions
                    Debug.Assert(Contains(Settings.DefaultPartition, key) == result.HasValue);
                    Debug.Assert(DefaultPartitionContains(key) == result.HasValue);
                    return result;
                }
                catch (Exception ex)
                {
                    LastError = ex;
                    Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, Settings.DefaultPartition, key), ex);
                    return Option.None<object>();
                }
            }
        }

        /// <summary>
        ///   Adds a "sliding" value with given partition and key. Value will last as much as
        ///   specified in given interval and, if accessed before expiry, its lifetime will be
        ///   extended by the interval itself.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public void AddSliding<TVal>(string partition, string key, TVal value, TimeSpan interval, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                AddInternal(partition, key, value, Clock.UtcNow + interval, interval, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == interval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "sliding" value with given key and default partition. Value will last as much as
        ///   specified in given interval and, if accessed before expiry, its lifetime will be
        ///   extended by the interval itself.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public void AddSlidingToDefaultPartition<TVal>(string key, TVal value, TimeSpan interval, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                AddInternal(Settings.DefaultPartition, key, value, Clock.UtcNow + interval, interval, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == interval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, Settings.DefaultPartition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "static" value with given partition and key. Value will last as much as
        ///   specified in <see cref="AbstractCacheSettings.StaticIntervalInDays"/> and, if accessed
        ///   before expiry, its lifetime will be extended by that interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public void AddStatic<TVal>(string partition, string key, TVal value, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                AddInternal(partition, key, value, Clock.UtcNow + Settings.StaticInterval, Settings.StaticInterval, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == Settings.StaticInterval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "static" value with given key and default partition. Value will last as much as
        ///   specified in <see cref="AbstractCacheSettings.StaticIntervalInDays"/> and, if accessed
        ///   before expiry, its lifetime will be extended by that interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public void AddStaticToDefaultPartition<TVal>(string key, TVal value, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                AddInternal(Settings.DefaultPartition, key, value, Clock.UtcNow + Settings.StaticInterval, Settings.StaticInterval, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == Settings.StaticInterval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, Settings.DefaultPartition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "timed" value with given partition and key. Value will last until the specified
        ///   time and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public void AddTimed<TVal>(string partition, string key, TVal value, DateTime utcExpiry, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                AddInternal(partition, key, value, utcExpiry, TimeSpan.Zero, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "timed" value with given key and default partition. Value will last until the
        ///   specified time and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public void AddTimedToDefaultPartition<TVal>(string key, TVal value, DateTime utcExpiry, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                AddInternal(Settings.DefaultPartition, key, value, utcExpiry, TimeSpan.Zero, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, Settings.DefaultPartition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "timed" value with given partition and key. Value will last for the specified
        ///   lifetime and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public void AddTimed<TVal>(string partition, string key, TVal value, TimeSpan lifetime, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                AddInternal(partition, key, value, Clock.UtcNow.Add(lifetime), TimeSpan.Zero, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "timed" value with given key and default partition. Value will last for the
        ///   specified lifetime and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public void AddTimedToDefaultPartition<TVal>(string key, TVal value, TimeSpan lifetime, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                AddInternal(Settings.DefaultPartition, key, value, Clock.UtcNow.Add(lifetime), TimeSpan.Zero, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, Settings.DefaultPartition, key), ex);
            }
        }

        /// <summary>
        ///   Clears this instance, that is, it removes all values.
        /// </summary>
        /// <returns>The number of items that have been removed.</returns>
        public long Clear()
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = ClearInternal(null);

                // Postconditions
                Debug.Assert(result >= 0L);
                Debug.Assert(Count() == 0);
                Debug.Assert(LongCount() == 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnClearAll, ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Clears given partition, that is, it removes all its values.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <returns>The number of items that have been removed.</returns>
        public long Clear(string partition)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);

            try
            {
                var result = ClearInternal(partition);

                // Postconditions
                Debug.Assert(result >= 0L);
                Debug.Assert(Count(partition) == 0);
                Debug.Assert(LongCount(partition) == 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnClearPartition, partition), ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Clears default partition, that is, it removes all its values.
        /// </summary>
        /// <returns>The number of items that have been removed.</returns>
        public long ClearDefaultPartition()
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = ClearInternal(Settings.DefaultPartition);

                // Postconditions
                Debug.Assert(result >= 0L);
                Debug.Assert(Count(Settings.DefaultPartition) == 0);
                Debug.Assert(LongCount(Settings.DefaultPartition) == 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnClearPartition, Settings.DefaultPartition), ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public bool Contains(string partition, string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                return ContainsInternal(partition, key);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return false;
            }
        }

        /// <summary>
        ///   Determines whether cache contains the specified key in the default partition.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified key in the default partition.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public bool DefaultPartitionContains(string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                return ContainsInternal(Settings.DefaultPartition, key);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, Settings.DefaultPartition, key), ex);
                return false;
            }
        }

        /// <summary>
        ///   The number of items in the cache.
        /// </summary>
        /// <returns>The number of items in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public int Count()
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = Convert.ToInt32(CountInternal(null));

                // Postconditions
                Debug.Assert(result >= 0);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnCountAll, ex);
                return 0;
            }
        }

        /// <summary>
        ///   The number of items in given partition.
        /// </summary>
        /// <param name="partition"></param>
        /// <returns>The number of items in given partition.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public int Count(string partition)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);

            try
            {
                var result = Convert.ToInt32(CountInternal(partition));

                // Postconditions
                Debug.Assert(result >= 0);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnCountPartition, partition), ex);
                return 0;
            }
        }

        /// <summary>
        ///   The number of items in default partition.
        /// </summary>
        /// <returns>The number of items in default partition.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public int DefaultPartitionCount()
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = Convert.ToInt32(CountInternal(Settings.DefaultPartition));

                // Postconditions
                Debug.Assert(result >= 0);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnCountPartition, Settings.DefaultPartition), ex);
                return 0;
            }
        }

        /// <summary>
        ///   The number of items in the cache.
        /// </summary>
        /// <returns>The number of items in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public long LongCount()
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = CountInternal(null);

                // Postconditions
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnCountAll, ex);
                return 0L;
            }
        }

        /// <summary>
        ///   The number of items in given partition.
        /// </summary>
        /// <param name="partition"></param>
        /// <returns>The number of items in given partition.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public long LongCount(string partition)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);

            try
            {
                var result = CountInternal(partition);

                // Postconditions
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnCountPartition, partition), ex);
                return 0L;
            }
        }

        /// <summary>
        ///   The number of items in default partition.
        /// </summary>
        /// <returns>The number of items in default partition.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public long DefaultPartitionLongCount()
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = CountInternal(Settings.DefaultPartition);

                // Postconditions
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnCountPartition, Settings.DefaultPartition), ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by the corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        public Option<TVal> Get<TVal>(string partition, string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                var result = GetInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return Option.None<TVal>();
            }
        }

        /// <summary>
        ///   Gets the value with default partition and specified key. If it is a "sliding" or
        ///   "static" value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The value with default partition and specified key.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        public Option<TVal> GetFromDefaultPartition<TVal>(string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                var result = GetInternal<TVal>(Settings.DefaultPartition, key);

                // Postconditions
                Debug.Assert(Contains(Settings.DefaultPartition, key) == result.HasValue);
                Debug.Assert(DefaultPartitionContains(key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, Settings.DefaultPartition, key), ex);
                return Option.None<TVal>();
            }
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        public Option<CacheItem<TVal>> GetItem<TVal>(string partition, string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                var result = GetItemInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return Option.None<CacheItem<TVal>>();
            }
        }

        /// <summary>
        ///   Gets the cache item with default partition and specified key. If it is a "sliding" or
        ///   "static" value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with default partition and specified key.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        public Option<CacheItem<TVal>> GetItemFromDefaultPartition<TVal>(string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                var result = GetItemInternal<TVal>(Settings.DefaultPartition, key);

                // Postconditions
                Debug.Assert(Contains(Settings.DefaultPartition, key) == result.HasValue);
                Debug.Assert(DefaultPartitionContains(key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, Settings.DefaultPartition, key), ex);
                return Option.None<CacheItem<TVal>>();
            }
        }

        /// <summary>
        ///   Gets all cache items. If an item is a "sliding" or "static" value, its lifetime will be
        ///   increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All cache items.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        public CacheItem<TVal>[] GetItems<TVal>()
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = GetItemsInternal<TVal>(null);

                // Postconditions
                Debug.Assert(result != null);
                Debug.Assert(result.Length == Count());
                Debug.Assert(result.LongLength == LongCount());
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnReadAll, ex);
                return new CacheItem<TVal>[0];
            }
        }

        /// <summary>
        ///   Gets all cache items in given partition. If an item is a "sliding" or "static" value,
        ///   its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <returns>All cache items in given partition.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        public CacheItem<TVal>[] GetItems<TVal>(string partition)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);

            try
            {
                var result = GetItemsInternal<TVal>(partition);

                // Postconditions
                Debug.Assert(result != null);
                Debug.Assert(result.Length == Count(partition));
                Debug.Assert(result.LongLength == LongCount(partition));
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnReadPartition, partition), ex);
                return new CacheItem<TVal>[0];
            }
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "sliding" value with given partition and key.
        ///   Value will last as much as specified in given interval and, if accessed before expiry,
        ///   its lifetime will be extended by the interval itself.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="interval">The interval.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public TVal GetOrAddSliding<TVal>(string partition, string key, Func<TVal> valueGetter, TimeSpan interval, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                var result = GetInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = valueGetter.Invoke();
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                AddInternal(partition, key, value, Clock.UtcNow + interval, interval, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == interval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with default partition and specified key. If
        ///   it is a "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "sliding" value with given key and default
        ///   partition. Value will last as much as specified in given interval and, if accessed
        ///   before expiry, its lifetime will be extended by the interval itself.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="interval">The interval.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public TVal GetOrAddSlidingToDefaultPartition<TVal>(string key, Func<TVal> valueGetter, TimeSpan interval, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                var result = GetInternal<TVal>(Settings.DefaultPartition, key);

                // Postconditions
                Debug.Assert(Contains(Settings.DefaultPartition, key) == result.HasValue);
                Debug.Assert(DefaultPartitionContains(key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, Settings.DefaultPartition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = valueGetter.Invoke();
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                AddInternal(Settings.DefaultPartition, key, value, Clock.UtcNow + interval, interval, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == interval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, Settings.DefaultPartition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "static" value with given partition and key.
        ///   Value will last as much as specified in <see
        ///   cref="P:PommaLabs.KVLite.Core.AbstractCacheSettings.StaticIntervalInDays"/> and, if
        ///   accessed before expiry, its lifetime will be extended by that interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public TVal GetOrAddStatic<TVal>(string partition, string key, Func<TVal> valueGetter, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                var result = GetInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = valueGetter.Invoke();
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                AddInternal(partition, key, value, Clock.UtcNow + Settings.StaticInterval, Settings.StaticInterval, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == Settings.StaticInterval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with default partition and specified key. If
        ///   it is a "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "static" value with given key and default
        ///   partition. Value will last as much as specified in <see
        ///   cref="P:PommaLabs.KVLite.Core.AbstractCacheSettings.StaticIntervalInDays"/> and, if
        ///   accessed before expiry, its lifetime will be extended by that interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public TVal GetOrAddStaticToDefaultPartition<TVal>(string key, Func<TVal> valueGetter, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                var result = GetInternal<TVal>(Settings.DefaultPartition, key);

                // Postconditions
                Debug.Assert(Contains(Settings.DefaultPartition, key) == result.HasValue);
                Debug.Assert(DefaultPartitionContains(key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, Settings.DefaultPartition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = valueGetter.Invoke();
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                AddInternal(Settings.DefaultPartition, key, value, Clock.UtcNow + Settings.StaticInterval, Settings.StaticInterval, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == Settings.StaticInterval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, Settings.DefaultPartition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "timed" value with given partition and key.
        ///   Value will last until the specified time and, if accessed before expiry, its lifetime
        ///   will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public TVal GetOrAddTimed<TVal>(string partition, string key, Func<TVal> valueGetter, DateTime utcExpiry, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                var result = GetInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = valueGetter.Invoke();
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                AddInternal(partition, key, value, utcExpiry, TimeSpan.Zero, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with default partition and specified key. If
        ///   it is a "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "timed" value with given key and default
        ///   partition. Value will last until the specified time and, if accessed before expiry, its
        ///   lifetime will _not_ be extended.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public TVal GetOrAddTimedToDefaultPartition<TVal>(string key, Func<TVal> valueGetter, DateTime utcExpiry, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                var result = GetInternal<TVal>(Settings.DefaultPartition, key);

                // Postconditions
                Debug.Assert(Contains(Settings.DefaultPartition, key) == result.HasValue);
                Debug.Assert(DefaultPartitionContains(key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, Settings.DefaultPartition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = valueGetter.Invoke();
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                AddInternal(Settings.DefaultPartition, key, value, utcExpiry, TimeSpan.Zero, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, Settings.DefaultPartition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "timed" value with given partition and key.
        ///   Value will last for the specified lifetime and, if accessed before expiry, its lifetime
        ///   will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public TVal GetOrAddTimed<TVal>(string partition, string key, Func<TVal> valueGetter, TimeSpan lifetime, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                var result = GetInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = valueGetter.Invoke();
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                AddInternal(partition, key, value, Clock.UtcNow.Add(lifetime), TimeSpan.Zero, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with default partition and specified key. If
        ///   it is a "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "timed" value with given key and default
        ///   partition. Value will last for the specified lifetime and, if accessed before expiry,
        ///   its lifetime will _not_ be extended.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the <see
        ///   cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public TVal GetOrAddTimedToDefaultPartition<TVal>(string key, Func<TVal> valueGetter, TimeSpan lifetime, IList<string> parentKeys = null)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);
            
            try
            {
                var result = GetInternal<TVal>(Settings.DefaultPartition, key);

                // Postconditions
                Debug.Assert(Contains(Settings.DefaultPartition, key) == result.HasValue);
                Debug.Assert(DefaultPartitionContains(key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, Settings.DefaultPartition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = valueGetter.Invoke();
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                AddInternal(Settings.DefaultPartition, key, value, Clock.UtcNow.Add(lifetime), TimeSpan.Zero, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key) || !CanPeek || PeekItem<TVal>(Settings.DefaultPartition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, Settings.DefaultPartition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   Gets the value corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   The value corresponding to given partition and key, without updating expiry date.
        /// </returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        public Option<TVal> Peek<TVal>(string partition, string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseNotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = PeekInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return Option.None<TVal>();
            }
        }

        /// <summary>
        ///   Gets the value corresponding to default partition and given key, without updating
        ///   expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   The value corresponding to default partition and given key, without updating expiry date.
        /// </returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        public Option<TVal> PeekIntoDefaultPartition<TVal>(string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseNotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = PeekInternal<TVal>(Settings.DefaultPartition, key);

                // Postconditions
                Debug.Assert(Contains(Settings.DefaultPartition, key) == result.HasValue);
                Debug.Assert(DefaultPartitionContains(key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, Settings.DefaultPartition, key), ex);
                return Option.None<TVal>();
            }
        }

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        public Option<CacheItem<TVal>> PeekItem<TVal>(string partition, string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseNotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = PeekItemInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return Option.None<CacheItem<TVal>>();
            }
        }

        /// <summary>
        ///   Gets the item corresponding to default partition and given key, without updating expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   The item corresponding to default partition and givne key, without updating expiry date.
        /// </returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        public Option<CacheItem<TVal>> PeekItemIntoDefaultPartition<TVal>(string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            RaiseNotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = PeekItemInternal<TVal>(Settings.DefaultPartition, key);

                // Postconditions
                Debug.Assert(Contains(Settings.DefaultPartition, key) == result.HasValue);
                Debug.Assert(DefaultPartitionContains(key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, Settings.DefaultPartition, key), ex);
                return Option.None<CacheItem<TVal>>();
            }
        }

        /// <summary>
        ///   Gets the all values, without updating expiry dates.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All values, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        public CacheItem<TVal>[] PeekItems<TVal>()
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseNotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = PeekItemsInternal<TVal>(null);

                // Postconditions
                Debug.Assert(result != null);
                Debug.Assert(result.Length == Count());
                Debug.Assert(result.LongLength == LongCount());
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnReadAll, ex);
                return new CacheItem<TVal>[0];
            }
        }

        /// <summary>
        ///   Gets the all items in given partition, without updating expiry dates.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <returns>All items in given partition, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="object"/> as type parameter; that will work whether the required value is a class
        ///   or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        public CacheItem<TVal>[] PeekItems<TVal>(string partition)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseNotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = PeekItemsInternal<TVal>(partition);

                // Postconditions
                Debug.Assert(result != null);
                Debug.Assert(result.Length == Count(partition));
                Debug.Assert(result.LongLength == LongCount(partition));
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnReadPartition, partition), ex);
                return new CacheItem<TVal>[0];
            }
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        public void Remove(string partition, string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                RemoveInternal(partition, key);

                // Postconditions
                Debug.Assert(!Contains(partition, key));
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Removes the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public void RemoveFromDefaultPartition(string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                RemoveInternal(Settings.DefaultPartition, key);

                // Postconditions
                Debug.Assert(!Contains(Settings.DefaultPartition, key));
                Debug.Assert(!DefaultPartitionContains(key));
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, Settings.DefaultPartition, key), ex);
            }
        }

        #endregion ICache members
    }
}