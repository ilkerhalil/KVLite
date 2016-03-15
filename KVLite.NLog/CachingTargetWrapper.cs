using NLog.Targets.Wrappers;

namespace PommaLabs.KVLite.NLog
{
    [Target("CachingWrapper", IsWrapper = true)]
    public sealed class CachingTargetWrapper : WrapperTargetBase
    {
    }
}