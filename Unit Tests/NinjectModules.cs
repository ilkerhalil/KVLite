using Finsa.CodeServices.Clock;

namespace UnitTests
{
    /// <summary>
    ///   Bindings for KVLite.
    /// </summary>
    internal sealed class KVLiteModule : Ninject.Modules.NinjectModule
    {
        public override void Load()
        {
            Bind<IClock>().To<MockClock>();
        }
    }
}