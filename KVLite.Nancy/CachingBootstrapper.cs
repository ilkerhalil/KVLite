// File name: CachingBootstrapper.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
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
using System.Collections.Generic;
using System.IO;
using Common.Logging;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace PommaLabs.KVLite.Nancy
{
    /// <summary>
    ///   A Nancy bootstrapper which implements KVLite-based caching, both persistent and volatile.
    ///   You can configure it using application settings.
    /// </summary>
    [CLSCompliant(false)]
    public abstract class CachingBootstrapper : DefaultNancyBootstrapper
    {
        #region Fields

        /// <summary>
        ///   The partition used by Nancy response cache items.
        /// </summary>
        const string ResponseCachePartition = "KVLite.Nancy.ResponseCache";

        readonly ICache _cache;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="CachingBootstrapper"/> class. You can
        ///   optionally instruct the bootstrapper on which cache instance it should use; by
        ///   default, it uses <see cref="PersistentCache.DefaultInstance"/>.
        /// </summary>
        /// <param name="cache">
        ///   The cache that will be used as entry container. If <paramref name="cache"/> is null,
        ///   then <see cref="PersistentCache.DefaultInstance"/> will be used instead.
        /// </param>
        protected CachingBootstrapper(ICache cache = null)
        {
            _cache = cache ?? PersistentCache.DefaultInstance;
        }

        #endregion Construction

        #region Properties

        /// <summary>
        ///   Gets the cache instance currently used by the bootstrapper.
        /// </summary>
        /// <value>The cache instance currently used by the bootstrapper.</value>
        public ICache Cache { get; }

        #endregion Properties

        /// <summary>
        ///   Handles application startup.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="pipelines">The pipelines.</param>
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            pipelines.BeforeRequest += CheckCache;
            pipelines.AfterRequest += SetCache;
        }

        /// <summary>
        ///   Check to see if we have a cache entry - if we do, see if it has expired or not, if it
        ///   hasn't then return it, otherwise return null.
        /// </summary>
        /// <param name="context">Current context.</param>
        /// <returns>Response or null.</returns>
        Response CheckCache(NancyContext context)
        {
            var cacheKey = context.GetRequestFingerprint();
            var cachedSummary = _cache.Get<ResponseSummary>(ResponseCachePartition, cacheKey);
            return cachedSummary.HasValue ? cachedSummary.Value.ToResponse() : null;
        }

        /// <summary>
        ///   Adds the current response to the cache if required Only stores by Path and stores the
        ///   response in a KVLite cache.
        /// </summary>
        /// <param name="context">Current context.</param>
        void SetCache(NancyContext context)
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

            // Disable further caching, as it must explicitly enabled.
            context.Items.Remove(ContextExtensions.OutputCacheTimeKey);

            // The response we are going to cache. We put it here, so that it can be used as
            // recovery in the catch clause below.
            var responseToBeCached = context.Response;

            try
            {
                var cacheKey = context.GetRequestFingerprint();
                var cachedSummary = new ResponseSummary(responseToBeCached);
                _cache.AddTimedAsync(ResponseCachePartition, cacheKey, cachedSummary, _cache.Clock.UtcNow.AddSeconds(cacheSeconds));
                context.Response = cachedSummary.ToResponse();
            }
            catch (Exception ex)
            {
                const string errMsg = "Something bad happened while caching :-(";
                LogManager.GetLogger<CachingBootstrapper>().Error(errMsg, ex);
                // Sets the old response, hoping it will work...
                context.Response = responseToBeCached;
            }
        }

        [Serializable]
        sealed class ResponseSummary
        {
            readonly string _contentType;
            readonly IDictionary<string, string> _headers;
            readonly HttpStatusCode _statusCode;
            readonly byte[] _contents;

            public ResponseSummary(Response response)
            {
                _contentType = response.ContentType;
                _headers = response.Headers;
                _statusCode = response.StatusCode;

                using (var memoryStream = new MemoryStream())
                {
                    response.Contents(memoryStream);
                    _contents = memoryStream.ToArray();
                }
            }

            public Response ToResponse()
            {
                return new Response
                {
                    ContentType = _contentType,
                    Headers = _headers,
                    StatusCode = _statusCode,
                    Contents = stream => stream.Write(_contents, 0, _contents.Length)
                };
            }
        }
    }
}
