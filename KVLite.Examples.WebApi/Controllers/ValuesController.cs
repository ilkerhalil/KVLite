using System.Collections.Generic;
using System.Web.Http;
using WebApi.OutputCache.V2;

namespace RestService.WebApi.Controllers
{
    [RoutePrefix("values")]
    public class ValuesController : ApiController
    {
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
        public void Post([FromBody]string value)
        {
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