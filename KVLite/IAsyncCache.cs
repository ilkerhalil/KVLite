using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite
{
    public interface IAsyncCache<out TCacheSettings> : ICache<TCacheSettings>
        where TCacheSettings : AbstractCacheSettings
    {
    }
}
