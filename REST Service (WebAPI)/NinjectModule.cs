using PommaLabs.KVLite;

namespace RestService.Mvc
{
    public sealed class NinjectModule : Ninject.Modules.NinjectModule
    {
        public override void Load()
        {
            Bind<ICache>().To<PersistentCache>();
        }
    }
}
