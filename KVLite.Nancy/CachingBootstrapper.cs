// File name: CachingBootstrapper.cs
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

using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Logging;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.IO;

namespace PommaLabs.KVLite.Nancy
{
    /// <summary>
    ///   A Nancy bootstrapper which implements KVLite-based caching, both persistent and volatile.
    ///   You can configure it using application settings.
    /// </summary>
    public abstract class CachingBootstrapper : DefaultNancyBootstrapper
    {
        #region Fields

        /// <summary>
        ///   The partition used by Nancy response cache items.
        /// </summary>
        private static readonly string ResponseCachePartition = $"{CachePartitions.Prefix}.NancyResponses";

        private static readonly ILog Log = LogProvider.For<CachingBootstrapper>();

        private readonly ICache _cache;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="CachingBootstrapper"/> class. You need to
        ///   instruct the bootstrapper on which cache instance it should use.
        /// </summary>
        /// <param name="cache">The cache that will be used as entry container.</param>
        protected CachingBootstrapper(ICache cache)
        {
            // Preconditions
            Raise.ArgumentNullException.IfIsNull(cache, nameof(cache), ErrorMessages.NullCache);

            _cache = cache;
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
        private Response CheckCache(NancyContext context)
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
        private void SetCache(NancyContext context)
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

            // The response we are going to cache. We put it here, so that it can be used as recovery
            // in the catch clause below.
            var responseToBeCached = context.Response;

            try
            {
                var cacheKey = context.GetRequestFingerprint();
                var cachedSummary = new ResponseSummary(responseToBeCached);
                _cache.AddTimed(ResponseCachePartition, cacheKey, cachedSummary, _cache.Clock.UtcNow.AddSeconds(cacheSeconds));
                context.Response = cachedSummary.ToResponse();
            }
            catch (Exception ex)
            {
                Log.ErrorException("Something bad happened while caching :-(", ex);
                // Sets the old response, hoping it will work...
                context.Response = responseToBeCached;
            }
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
                    response.Contents(memoryStream);
                    _contents = memoryStream.ToArray();
                }
            }

            public Response ToResponse() => new Response
            {
                ContentType = _contentType,
                Headers = _headers,
                StatusCode = _statusCode,
                Contents = stream => stream.Write(_contents, 0, _contents.Length)
            };
        }
    }
}
