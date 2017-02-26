// File name: UnixClockExtensions.cs
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

namespace PommaLabs.KVLite.Extensibility
{
    /// <summary>
    ///   Converts standard date and time from and to UNIX time.
    /// </summary>
    public static class UnixClockExtensions
    {
        /// <summary>
        ///   The UNIX epoch (https://en.wikipedia.org/wiki/Unix_time).
        /// </summary>
        private static DateTime UnixEpoch { get; } = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        ///   Converts given <see cref="DateTime"/> into UNIX time (seconds since UNIX epoch).
        /// </summary>
        /// <param name="dt">The date that should be converted.</param>
        /// <returns>Given <see cref="DateTime"/> converted into UNIX time.</returns>
        public static long ToUnixTime(this DateTime dt) => (long) dt.Subtract(UnixEpoch).TotalSeconds;

        /// <summary>
        ///   Returns current UNIX time (seconds since UNIX epoch).
        /// </summary>
        /// <param name="clock">The clock used to retrieve current date and time.</param>
        /// <returns>Current UNIX time (seconds since UNIX epoch).</returns>
        public static long ToUnixTime(this IClock clock) => (long) clock.UtcNow.Subtract(UnixEpoch).TotalSeconds;

        /// <summary>
        ///   Converts given UNIX time into a valid UTC date and time.
        /// </summary>
        /// <param name="unixTime">UNIX time.</param>
        /// <returns>A valid UTC date and time corresponding to given UNIX time.</returns>
        public static DateTime ToUtcDateTime(this long unixTime) => UnixEpoch.AddSeconds(unixTime);
    }
}
