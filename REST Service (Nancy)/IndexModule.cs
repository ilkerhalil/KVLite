using Nancy;
using PommaLabs.KVLite.Nancy;

namespace RestService.Nancy
{
    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            Get["/"] = parameters => {
                const int cacheSeconds = 120;
                Context.EnableOutputCache(cacheSeconds);
                return View["index"];
            };

            Post["/"] = parameters => {
                const int cacheSeconds = 30;
                Context.EnableOutputCache(cacheSeconds);
                return View["index"];
            };

            Get["/values"] = parameters =>
            {
                const int cacheSeconds = 30;
                Context.EnableOutputCache(cacheSeconds);
                return Response.AsJson(new[] {1, 2, 3});
            };
        }
    }
}