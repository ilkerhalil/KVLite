using PommaLabs.CodeServices.Caching;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.Web.Http;
using WebApi.OutputCache.V2;

namespace RestService.WebApi.Controllers
{
    [RoutePrefix("values")]
    public class ValuesController : ApiController
    {
        private readonly ICache _cache;

        public ValuesController(ICache cache)
        {
            Raise.ArgumentNullException.IfIsNull(cache, nameof(cache));
            _cache = cache;
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
