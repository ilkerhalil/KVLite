using System.Web.Http;
using PommaLabs.KVLite;
using PommaLabs.KVLite.Web.Http;

namespace RestService.WebApi.Controllers
{
    [RoutePrefix("cache")]
    public sealed class CacheController : AbstractCacheController
    {
        public CacheController(ICache cache) : base(cache)
        {
        }
    }
}