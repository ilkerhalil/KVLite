// File name: ClockHelper.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2018 Alessio Parma <alessio.parma@gmail.com>
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

namespace PommaLabs.KVLite.Extensibility
{
    /// <summary>
    ///   Helpers for clock management.
    /// </summary>
    public static class ClockHelper
    {
        private static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);

        /// <summary>
        ///   Converts a Unix time expressed as the number of seconds that have elapsed since
        ///   1970-01-01T00:00:00Z to a System.DateTimeOffset value.
        /// </summary>
        /// <param name="seconds">Seconds.</param>
        /// <returns>
        ///   A date and time value that represents the same moment in time as the Unix time.
        /// </returns>
        public static DateTimeOffset FromUnixTimeSeconds(long seconds) => UnixEpoch.AddSeconds(seconds);

        /// <summary>
        ///   Returns the number of seconds that have elapsed since 1970-01-01T00:00:00Z.
        /// </summary>
        /// <param name="dateTimeOffset">Date time offset.</param>
        /// <returns>The number of seconds that have elapsed since 1970-01-01T00:00:00Z.</returns>
        public static long ToUnixTimeSeconds(DateTimeOffset dateTimeOffset) => (long) dateTimeOffset.Subtract(UnixEpoch).TotalSeconds;
    }
}
