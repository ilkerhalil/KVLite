// File name: CacheResult.cs
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

using PommaLabs.KVLite.Resources;
using PommaLabs.KVLite.Thrower;
using System;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   The result of a cache read operation.
    /// </summary>
    /// <typeparam name="T">The requested type.</typeparam>
    public struct CacheResult<T>
    {
        private readonly T _value;

        /// <summary>
        ///   Builds a cache result.
        /// </summary>
        /// <param name="value">Result value.</param>
        private CacheResult(T value)
        {
            HasValue = true;
            _value = value;
        }

        /// <summary>
        ///   Result value.
        /// </summary>
        /// <exception cref="InvalidOperationException">Result has no value.</exception>
        public T Value
        {
            get
            {
                // Preconditions
                Raise.InvalidOperationException.If(HasNoValue, ErrorMessages.EmptyCacheResult);

                return _value;
            }
        }

        /// <summary>
        ///   True if result has a value, false otherwise.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        ///   True if result has no value, false otherwise.
        /// </summary>
        public bool HasNoValue => !HasValue;

        /// <summary>
        ///   Result value if it exists, otherwise it returns default value for given type.
        /// </summary>
        /// <returns>
        ///   Result value if it exists, otherwise it returns default value for given type.
        /// </returns>
        public T ValueOrDefault() => HasValue ? _value : default(T);

        /// <summary>
        ///   Creates a result from given value.
        /// </summary>
        /// <param name="value">The value.</param>
        public static implicit operator CacheResult<T>(T value) => new CacheResult<T>(value);
    }
}
