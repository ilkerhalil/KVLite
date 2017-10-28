// File name: CachingEnumerable.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parma@gmail.com>
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

using NodaTime;
using PommaLabs.KVLite.Resources;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PommaLabs.KVLite.Goodies
{
    /// <summary>
    ///   A caching enumerable which stores huge collections in timed cache pages.
    /// </summary>
    /// <typeparam name="T">The type of the items stored in the caching collection.</typeparam>
    public sealed class CachingEnumerable<T> : IEnumerable<T>, IDisposable
    {
        /// <summary>
        ///   Default page size of cached collection pages.
        /// </summary>
        private const int PrivateDefaultPageSize = 100;

        private readonly ICache _cache;
        private readonly int _pageSize;
        private readonly string _cachePartition;
        private readonly int _itemCount;
        private readonly int _pageCount;

        /// <summary>
        ///   Builds a caching enumerable starting from source collection.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="source">The source enumerable.</param>
        /// <param name="pageSize">The size of cache collection pages. Defaults to <see cref="DefaultPageSize"/>.</param>
        public CachingEnumerable(ICache cache, IEnumerable<T> source, int pageSize = PrivateDefaultPageSize)
        {
            // Preconditions
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

            _cache = cache ?? throw new ArgumentNullException(nameof(cache), ErrorMessages.NullCache);
            _pageSize = pageSize;

            // Fill cache starting from given enumerable.
            var result = FillCacheFromEnumerable(cache, source, pageSize);
            _cachePartition = result.CachePartition;
            _itemCount = result.ItemCount;
            _pageCount = result.PageCount;
        }

        /// <summary>
        ///   Default page size of cached collection pages.
        /// </summary>
        public static int DefaultPageSize => PrivateDefaultPageSize;

        /// <summary>
        ///   The number of items stored inside the caching enumerable.
        /// </summary>
        public int Count => _itemCount;

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _pageCount; ++i)
            {
                var page = _cache.Get<T[]>(_cachePartition, ToCacheKey(i)).Value;

                foreach (var item in page)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///   An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region Private methods

        private static ImportResult FillCacheFromEnumerable(ICache cache, IEnumerable<T> source, int pageSize)
        {
            var collectionSuffix = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var cachePartition = CachePartitions.CachingEnumerablePrefix + "." + collectionSuffix;

            var page = new T[pageSize];
            var pageNumber = 0;
            var pageIndex = 0;
            var itemCount = 0;

            foreach (var item in source)
            {
                itemCount++;
                page[pageIndex++] = item;

                if (pageIndex == pageSize)
                {
                    pageIndex = 0;
                    cache.AddTimed(cachePartition, ToCacheKey(pageNumber++), page, Duration.FromDays(1));
                }
            }

            if (pageIndex != 0)
            {
                Array.Resize(ref page, pageIndex);
                cache.AddTimed(cachePartition, ToCacheKey(pageNumber++), page, Duration.FromDays(1));
            }

            return new ImportResult
            {
                CachePartition = cachePartition,
                ItemCount = itemCount,
                PageCount = pageNumber
            };
        }

        private static string ToCacheKey(int pageNumber) => pageNumber.ToString();

        private sealed class ImportResult
        {
            public string CachePartition { get; set; }

            public int ItemCount { get; set; }

            public int PageCount { get; set; }
        }

        #endregion Private methods

        #region IDisposable support

        private bool _disposed; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cache.Clear(_cachePartition);
                }
                _disposed = true;
            }
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting
        ///   unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion IDisposable support
    }
}
