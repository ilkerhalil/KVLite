using Microsoft.IO;
using System.IO;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Efficiently manages <see cref="MemoryStream"/> instances.
    /// </summary>
    public static class MemoryStreamManager
    {
        /// <summary>
        ///   Memory stream manager shared instance.
        /// </summary>
        public static readonly RecyclableMemoryStreamManager Instance = new RecyclableMemoryStreamManager();
    }
}
