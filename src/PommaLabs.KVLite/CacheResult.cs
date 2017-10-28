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
using System;
using System.Collections.Generic;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   The result of a cache read operation.
    /// </summary>
    /// <typeparam name="T">The requested type.</typeparam>
    public struct CacheResult<T> : IEquatable<CacheResult<T>>
    {
        /// <summary>
        ///   Creates a result from given value.
        /// </summary>
        /// <param name="value">The value.</param>
        public static implicit operator CacheResult<T>(T value) => new CacheResult<T>(value);

        /// <summary>
        ///   Backing field for <see cref="Value"/>.
        /// </summary>
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
        ///   True if result has a value, false otherwise.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        ///   Result value.
        /// </summary>
        /// <exception cref="InvalidOperationException">Result has no value.</exception>
        public T Value
        {
            get
            {
                // Preconditions
                if (!HasValue) throw new InvalidOperationException(ErrorMessages.EmptyCacheResult);

                return _value;
            }
        }

        /// <summary>
        ///   Compares two objects.
        /// </summary>
        /// <param name="x">Left.</param>
        /// <param name="y">Right.</param>
        /// <returns>True if they are not equal, false otherwise.</returns>
        public static bool operator !=(CacheResult<T> x, CacheResult<T> y) => !x.Equals(y);

        /// <summary>
        ///   Compares two objects.
        /// </summary>
        /// <param name="x">Left.</param>
        /// <param name="y">Right.</param>
        /// <returns>True if they are equal, false otherwise.</returns>
        public static bool operator ==(CacheResult<T> x, CacheResult<T> y) => x.Equals(y);

        /// <summary>
        ///   Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///   true if the current object is equal to the <paramref name="other">other</paramref>
        ///   parameter; otherwise, false.
        /// </returns>
        public bool Equals(CacheResult<T> other) => EqualityComparer<T>.Default.Equals(_value, other._value) && EqualityComparer<bool>.Default.Equals(HasValue, other.HasValue);

        /// <summary>
        ///   Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>
        ///   true if <paramref name="obj">obj</paramref> and this instance are the same type and
        ///   represent the same value; otherwise, false.
        /// </returns>
        public override bool Equals(object obj) => obj is CacheResult<T> && Equals((CacheResult<T>) obj);

        /// <summary>
        ///   Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                const int prime = -1521134295;
                var hash = 12345701;
                hash = hash * prime + EqualityComparer<T>.Default.GetHashCode(_value);
                hash = hash * prime + EqualityComparer<bool>.Default.GetHashCode(HasValue);
                return hash;
            }
        }

        /// <summary>
        ///   Result value if it exists, otherwise it returns default value for given type.
        /// </summary>
        /// <returns>
        ///   Result value if it exists, otherwise it returns default value for given type.
        /// </returns>
        public T ValueOrDefault() => HasValue ? _value : default(T);
    }
}
