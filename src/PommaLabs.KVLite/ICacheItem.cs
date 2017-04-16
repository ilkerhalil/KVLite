// File name: ICacheItem.cs
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

using System;
using System.Collections.Generic;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   An item that is stored in the cache.
    /// </summary>
    /// <typeparam name="TVal">The type of the value.</typeparam>
    public interface ICacheItem<TVal>
    {
        /// <summary>
        ///   The partition corresponding to this entry.
        /// </summary>
        string Partition { get; }

        /// <summary>
        ///   The key corresponding to this entry.
        /// </summary>
        string Key { get; }

        /// <summary>
        ///   The typed value.
        /// </summary>
        TVal Value { get; }

        /// <summary>
        ///   When the cache item was created.
        /// </summary>
        DateTime UtcCreation { get; }

        /// <summary>
        ///   When the cache item will expire.
        /// </summary>
        DateTime UtcExpiry { get; }

        /// <summary>
        ///   The refresh interval, used if the item is sliding; if it is not, it equals to <see cref="TimeSpan.Zero"/>.
        /// </summary>
        TimeSpan Interval { get; }

        /// <summary>
        ///   The parent keys of this item. If not specified, the array will be empty, but not null.
        /// </summary>
        IList<string> ParentKeys { get; }
    }
}
