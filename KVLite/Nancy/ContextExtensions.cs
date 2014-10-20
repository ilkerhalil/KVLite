//
// ContextExtensions.cs
// 
// Author(s):
//     Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Nancy;

namespace PommaLabs.KVLite.Nancy
{
    /// <summary>
    ///   TODO
    /// </summary>
    public static class ContextExtensions
    {
        internal const string OutputCacheTimeKey = "_output_cache_time_";
        internal const string OutputCacheArgsKey = "_output_cache_args_";

       /// <summary>
       ///   Enable output caching for this route.
       /// </summary>
       /// <param name="context">Current context.</param>
       /// <param name="seconds">Seconds to cache for.</param>
       public static void EnableOutputCache(this NancyContext context, int seconds)
        {
            context.Items[OutputCacheTimeKey] = seconds;
            context.Items[OutputCacheArgsKey] = null;
        }

       /// <summary>
       ///   Enable output caching for this route.
       /// </summary>
       /// <param name="context">Current context.</param>
       /// <param name="seconds">Seconds to cache for.</param>
       /// <param name="args">Arguments used to discriminate between requests at the same URL.</param>
       public static void EnableOutputCache<T>(this NancyContext context, int seconds, T args)
        {
            context.Items[OutputCacheTimeKey] = seconds;
            context.Items[OutputCacheArgsKey] = args;
        }

        /// <summary>
        ///   Disable the output cache for this route.
        /// </summary>
        /// <param name="context">Current context.</param>
        public static void DisableOutputCache(this NancyContext context)
        {
            context.Items.Remove(OutputCacheTimeKey);
        }
    }
}