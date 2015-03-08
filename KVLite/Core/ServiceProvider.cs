using PommaLabs.KVLite.Properties;
using PommaLabs.Reflection;
using PommaLabs.Testing;

namespace PommaLabs.KVLite.Core
{
    internal static class ServiceProvider
    {
        private static readonly IClockProvider CachedClock = ServiceLocator.Load<IClockProvider>(Settings.Default.AllCaches_DefaultClockProviderType);

        public static IClockProvider Clock
        {
            get { return CachedClock; }
        }
    }
}