// File name: StringUtils.cs
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
using System.IO;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Miscellaneous string utilities.
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        ///   Truncates given string if its length is greater than specified <paramref name="maxLength"/>.
        /// </summary>
        /// <param name="str">The string to be truncated.</param>
        /// <param name="maxLength">The length at which string should be truncated.</param>
        /// <returns>The first <paramref name="maxLength"/> characters of <paramref name="str"/>.</returns>
        public static string Truncate(this string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            maxLength = Math.Max(0, maxLength);
            return (str.Length < maxLength ? str : str.Substring(0, maxLength));
        }

        private static readonly string[] MapPathStarts = { "~//", "~\\\\", "~/", "~\\", "~" };

        /// <summary>
        ///   Maps given path into an absolute one.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Given path mapped into an absolute one.</returns>
        public static string MapPath(this string path)
        {
            // Preconditions
            if (path == null) throw new ArgumentNullException(nameof(path));

            if (Path.IsPathRooted(path))
            {
                return path;
            }
            var basePath = GetBaseDirectory();
            var trimmedPath = path.Trim();
            foreach (var start in MapPathStarts)
            {
                if (trimmedPath.StartsWith(start, StringComparison.Ordinal))
                {
                    trimmedPath = trimmedPath.Substring(start.Length, trimmedPath.Length - start.Length);
                    break;
                }
            }
            return Path.Combine(basePath, trimmedPath);
        }

        private static string GetBaseDirectory()
        {
#if NETSTD13
            return AppContext.BaseDirectory;
#else
            return AppDomain.CurrentDomain.BaseDirectory;
#endif
        }
    }
}
