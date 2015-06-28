// File name: ContextExtensions.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using Nancy;
using Finsa.CodeServices.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace PommaLabs.KVLite.Nancy
{
    /// <summary>
    ///   Nancy Context extensions to allow each Module to specify whether or not to enable caching.
    /// </summary>
    [CLSCompliant(false)]
    public static class ContextExtensions
    {
        internal const string OutputCacheTimeKey = "KVLite.Nancy.ResponseCacheTime";

        /// <summary>
        ///   Enable output caching for this route.
        /// </summary>
        /// <param name="context">Current context.</param>
        /// <param name="seconds">Seconds to cache for.</param>
        public static void EnableOutputCache(this NancyContext context, int seconds)
        {
            context.Items[OutputCacheTimeKey] = seconds;
        }

        /// <summary>
        ///   Disable the output cache for this route.
        /// </summary>
        /// <param name="context">Current context.</param>
        public static void DisableOutputCache(this NancyContext context)
        {
            context.Items.Remove(OutputCacheTimeKey);
        }

        #region Internal Methods

        private static readonly JsonSerializer RequestSerializer = new JsonSerializer();

        internal static string GetRequestFingerprint(this NancyContext context, Finsa.CodeServices.Serialization.ISerializer serializer)
        {
            var requestSummary = new { path = context.Request.Path, body = context.ReadAllBody() };
            var requestJson = RequestSerializer.SerializeToBytes(requestSummary);
            var requestHash = MD5.Create().ComputeHash(requestJson, 0, requestJson.Length);
            var fingerprint = new StringBuilder();
            foreach (var b in requestHash)
            {
                fingerprint.Append(b.ToString("X2"));
            }
            return fingerprint.ToString();
        }

        /// <summary>
        ///   </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static string ReadAllBody(this NancyContext context)
        {
            using (var streamReader = new StreamReader(context.Request.Body))
            {
                var body = streamReader.ReadToEnd();
                context.Request.Body.Seek(0, SeekOrigin.Begin);
                return body;
            }
        }

        #endregion Internal Methods
    }
}