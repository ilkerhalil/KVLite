using Common.Logging;
using Common.Logging.Simple;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using Ninject.Modules;
using PommaLabs.KVLite;
using PommaLabs.KVLite.CodeServices.Compression;

namespace RestService.WebApi
{
    internal sealed class NinjectConfig : NinjectModule
    {
        public override void Load()
        {
            Bind<IClock>().To<SystemClock>().InSingletonScope();
            Bind<ICompressor>().To<SnappyCompressor>().InSingletonScope();
            Bind<ILog>().To<NoOpLogger>().InSingletonScope();
            Bind<ICache>().To<PersistentCache>().InSingletonScope();
            Bind<ISerializer>().To<BinarySerializer>().InSingletonScope();
            Bind<BinarySerializerSettings>().ToMethod(ctx => new BinarySerializerSettings());
        }
    }
}
