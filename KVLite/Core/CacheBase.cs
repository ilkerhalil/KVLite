// File name: CacheBase.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for caches, implements common functionalities.
    /// </summary>
    /// <typeparam name="TCache">The type of the cache.</typeparam>
    /// <typeparam name="TCacheSettings">The type of the cache settings.</typeparam>
    [Serializable]
    public abstract class CacheBase<TCache, TCacheSettings> : ICache<TCache, TCacheSettings>
        where TCache : CacheBase<TCache, TCacheSettings>, ICache<TCache, TCacheSettings>, new()
        where TCacheSettings : CacheSettingsBase, new()
    {
        #region Fields

        private static readonly TCache CachedDefaultInstance = new TCache();

        private readonly TCacheSettings _settings;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   </summary>
        /// <param name="settings"></param>
        internal CacheBase(TCacheSettings settings)
        {
            Contract.Requires<ArgumentNullException>(settings != null);
            _settings = settings;
        }

        #endregion Construction

        #region Public Properties

        /// <summary>
        ///   TODO
        /// </summary>
        public static TCache DefaultInstance
        {
            get { return CachedDefaultInstance; }
        }

        public object this[string key]
        {
            get { return Get(Settings.DefaultPartition, key); }
        }

        #endregion Public Properties

        #region ICache Members

        public abstract CacheKind Kind { get; }
        
        CacheSettingsBase ICache.Settings
        {
            get { return _settings; }
        }

        public TCacheSettings Settings
        {
            get { return _settings; }
        }

        public object this[string partition, string key]
        {
            get { return Get(partition, key); }
        }

        public void AddSliding(string partition, string key, object value, TimeSpan interval)
        {
            DoAdd(partition, key, value, DateTime.UtcNow + interval, interval);
        }

        public void AddTimed(string partition, string key, object value, DateTime utcExpiry)
        {
            DoAdd(partition, key, value, utcExpiry, null);
        }

        public void Clear()
        {
            DoClear(null);
        }

        public void Clear(string partition)
        {
            DoClear(partition);
        }

        public long LongCount()
        {
            return DoCount(null);
        }

        public long LongCount(string partition)
        {
            return DoCount(partition);
        }

        public IList<CacheItem> GetManyItems()
        {
            return DoGetManyItems(null);
        }

        public IList<CacheItem> GetManyItems(string partition)
        {
            return DoGetManyItems(partition);
        }

        public IList<CacheItem> PeekManyItems()
        {
            return DoPeekManyItems(null);
        }

        public IList<CacheItem> PeekManyItems(string partition)
        {
            return DoPeekManyItems(partition);
        }

        #endregion ICache Members

        #region Abstract Methods

        public abstract bool Contains(string partition, string key);

        public abstract object Get(string partition, string key);

        public abstract CacheItem GetItem(string partition, string key);

        public abstract object Peek(string partition, string key);

        public abstract CacheItem PeekItem(string partition, string key);

        public abstract void Remove(string partition, string key);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="utcExpiry"></param>
        /// <param name="interval"></param>
        protected abstract void DoAdd(string partition, string key, object value, DateTime? utcExpiry, TimeSpan? interval);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        protected abstract void DoClear(string partition);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <returns></returns>
        protected abstract long DoCount(string partition);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <returns></returns>
        protected abstract IList<CacheItem> DoGetManyItems(string partition);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <returns></returns>
        protected abstract IList<CacheItem> DoPeekManyItems(string partition);

        #endregion Abstract Methods
    }
}