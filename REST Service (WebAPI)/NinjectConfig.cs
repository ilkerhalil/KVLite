using Finsa.CodeServices.Clock;
using Ninject.Modules;
using PommaLabs.KVLite;

namespace RestService.Mvc
{
    public sealed class NinjectConfig : NinjectModule
    {
        public override void Load()
        {
            Bind<IClock>().To<SystemClock>();
            Bind<ICache>().To<PersistentCache>();
        }
    }
}
