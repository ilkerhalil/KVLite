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

using PommaLabs.KVLite.Core;
using PommaLabs.Thrower;
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
