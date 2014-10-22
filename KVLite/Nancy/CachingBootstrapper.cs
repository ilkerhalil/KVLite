//
// CachingBootstrapper.cs
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

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace PommaLabs.KVLite.Nancy
{
    public class CachingBootstrapper : DefaultNancyBootstrapper
    {
        private const string NancyCachePartition = "KVLite.NancyResponseCache";

        private static ICache Cache
        {
            get
            {
                switch (Configuration.Instance.NancyCacheKind)
                {
                    case CacheKind.Persistent:
                        return PersistentCache.DefaultInstance;
                    case CacheKind.Volatile:
                        return null;
                    default:
                        throw new ConfigurationErrorsException();
                }
            }
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            pipelines.BeforeRequest += CheckCache;
            pipelines.AfterRequest += SetCache;
        }

        /// <summary>
        ///   Check to see if we have a cache entry - if we do, see if it has expired or not,
        ///   if it hasn't then return it, otherwise return null.
        /// </summary>
        /// <param name="context">Current context.</param>
        /// <returns>Response or null.</returns>
        public Response CheckCache(NancyContext context)
        {
            var cachedSummary = Cache[NancyCachePartition, context.Request.Path] as ResponseSummary;
            return (cachedSummary == null) ? null : cachedSummary.ToResponse();
        }

        /// <summary>
        ///   Adds the current response to the cache if required
        ///   Only stores by Path and stores the response in a KVLite cache.
        /// </summary>
        /// <param name="context">Current context.</param>
        public void SetCache(NancyContext context)
        {
            if (context.Response.StatusCode != HttpStatusCode.OK)
            {
                return;
            }

            object cacheSecondsObject;
            if (!context.Items.TryGetValue(ContextExtensions.OutputCacheTimeKey, out cacheSecondsObject))
            {
                return;
            }

            int cacheSeconds;
            if (!int.TryParse(cacheSecondsObject.ToString(), out cacheSeconds))
            {
                return;
            }

            var cachedSummary = new ResponseSummary(context.Response);

            Cache.AddTimedAsync(NancyCachePartition, context.Request.Path, cachedSummary, DateTime.UtcNow.AddSeconds(cacheSeconds));

            context.Response = cachedSummary.ToResponse();
        }
        
        [Serializable]
        private sealed class ResponseSummary
        {
            private readonly string _contentType;
            private readonly IDictionary<string, string> _headers;
            private readonly HttpStatusCode _statusCode;
            private readonly byte[] _contents;

            public ResponseSummary(Response response)
            {
                _contentType = response.ContentType;
                _headers = response.Headers;
                _statusCode = response.StatusCode;

                using (var memoryStream = new MemoryStream())
                {
                    response.Contents.Invoke(memoryStream);
                    _contents = memoryStream.GetBuffer();
                }
            }

            public Response ToResponse()
            {
                return new Response
                {
                    ContentType = _contentType,
                    Headers = _headers,
                    StatusCode = _statusCode,
                    Contents = stream =>
                    {
                        var writer = new StreamWriter(stream) { AutoFlush = true };
                        writer.Write(_contents);
                    }
                };
            }
        }
    }
}