using PommaLabs.KVLite;
using PommaLabs.KVLite.Nancy;

namespace RestService.Nancy
{
    public class Bootstrapper : CachingBootstrapper
    {
        // The bootstrapper enables you to reconfigure the composition of the framework, by
        // overriding the various methods and properties. For more information, see: "https://github.com/NancyFx/Nancy/wiki/Bootstrapper".
        public Bootstrapper() :
            base(PersistentCache.DefaultInstance)
        {
        }
    }
}