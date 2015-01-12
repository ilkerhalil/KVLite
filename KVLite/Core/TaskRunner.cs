using System;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.Core
{
    internal static class TaskRunner
    {
        public static Task Run(Action action)
        {
#if PORTABLE
            return Task.Run(action);
#else
            return TaskEx.Run(action);
#endif
        }
    }
}