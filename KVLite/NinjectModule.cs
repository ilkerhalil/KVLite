using Finsa.CodeServices.Clock;

namespace PommaLabs.KVLite
{
    public sealed class NinjectModule : Ninject.Modules.NinjectModule
    {
        public override void Load()
        {
            Bind<IClock>().To<SystemClock>();
        }
    }
}
