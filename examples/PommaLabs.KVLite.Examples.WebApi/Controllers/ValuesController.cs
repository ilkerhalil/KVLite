// File name: ValuesController.cs
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

using PommaLabs.KVLite.Resources;
using System;
using System.Collections.Generic;
using System.Web.Http;
using WebApi.OutputCache.V2;

namespace PommaLabs.KVLite.Examples.WebApi.Controllers
{
    /// <summary>
    ///   Example controller.
    /// </summary>
    [RoutePrefix("values")]
    public class ValuesController : ApiController
    {
        private readonly ICache _cache;

        public ValuesController(ICache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache), ErrorMessages.NullCache);
        }

        // GET api/values
        [Route(""), CacheOutput(ServerTimeSpan = 60)]
        public IEnumerable<string> Get()
        {
            return new[] { "value1", "value2" };
        }

        // GET api/values/5
        [Route("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [Route("")]
        public void Post([FromBody] string value)
        {
            _cache.AddStatic("WebApi", Guid.NewGuid().ToString(), value);
        }

        // PUT api/values/5
        [Route("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [Route("{id}")]
        public void Delete(int id)
        {
        }
    }
}
