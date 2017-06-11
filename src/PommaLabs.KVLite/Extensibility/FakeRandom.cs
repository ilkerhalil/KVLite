// File name: FakeRandom.cs
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

using PommaLabs.Thrower;
using System;

namespace PommaLabs.KVLite.Extensibility
{
    /// <summary>
    ///   Builds a fake random which constantly returns specified <see cref="Value"/>.
    /// </summary>
    public sealed class FakeRandom : IRandom
    {
        private double _value;

        /// <summary>
        ///   Sets <see cref="Value"/> to 0.5.
        /// </summary>
        public FakeRandom()
            : this(0.5)
        {
        }

        /// <summary>
        ///   Sets <see cref="Value"/> to <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The constant value.</param>
        public FakeRandom(double value)
        {
            Value = value;
        }

        /// <summary>
        ///   The value which will be returned by <see cref="NextDouble"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   Specified value is less than 0.0 or it is greater than or equal to 1.0.
        /// </exception>
        public double Value
        {
            get { return _value; }
            set
            {
                // Preconditions
                Raise.ArgumentOutOfRangeException.IfIsLess(value, 0.0, nameof(value));
                Raise.ArgumentOutOfRangeException.IfIsGreaterOrEqual(value, 1.0, nameof(value));

                _value = value;
            }
        }

        /// <summary>
        ///   Returns a specified floating-point number that is greater than or equal to 0.0, and
        ///   less than 1.0.
        /// </summary>
        /// <returns>
        ///   A double-precision floating point number that is greater than or equal to 0.0, and less
        ///   than 1.0.
        /// </returns>
        public double NextDouble() => _value;
    }
}
