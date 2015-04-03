using PommaLabs.Testing;

namespace UnitTests
{
    /// <summary>
    ///   Bindings for KVLite.
    /// </summary>
    internal sealed class KVLiteModule : Ninject.Modules.NinjectModule
    {
        public override void Load()
        {
            Bind<IClockService>().To<MockClockService>();
        }
    }
}