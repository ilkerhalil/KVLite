using PommaLabs.KVLite.Properties;
using PommaLabs.Reflection;
using PommaLabs.Testing;

namespace PommaLabs.KVLite.Core
{
    internal static class ServiceProvider
    {
        private static readonly IClockService CachedClock = ServiceLocator.Load<IClockService>(Settings.Default.AllCaches_DefaultClockServiceType);

        public static IClockService Clock
        {
            get { return CachedClock; }
        }
    }
}