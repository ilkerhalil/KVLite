using System;
using System.Threading;
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
            return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
#endif
        }
    }
}