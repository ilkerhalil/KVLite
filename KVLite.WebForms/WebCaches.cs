// File name: WebCaches.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Finsa.CodeServices.Serialization;

namespace PommaLabs.KVLite.WebForms
{
    /// <summary>
    ///   Class containing singletons for all cache types used inside the KVLite.Web branch.
    ///   Instances defined in this class should be used for ASP.NET WebForms projects.
    /// 
    ///   For example, all output cache providers and view state persisters make use of these
    ///   instances in order to perform thier caching jobs.
    /// 
    ///   Since all properties defined in this class can be written, you can use this class to
    ///   customize the behavior of all elements defined in the KVLite.Web branch.
    /// </summary>
    /// <remarks>
    ///   We use custom defitions because the KVLite.Web branch usually deals with objects that
    ///   require the <see cref="BinarySerializer"/> instead of the <see cref="JsonSerializer"/>,
    ///   which is the default serializer for all caches.
    /// </remarks>
    public static class WebCaches
    {
        /// <summary>
        ///   The default instance for <see cref="MemoryCache"/>.
        /// </summary>
        /// <remarks>
        ///   Here we use the default instance <see cref="MemoryCache.DefaultInstance"/>, because
        ///   memory cache does not serialize objects (the serializer is used only to produce unique keys).
        /// </remarks>
        public static MemoryCache Memory { get; set; } = MemoryCache.DefaultInstance;

        /// <summary>
        ///   The default instance for <see cref="PersistentCache"/>.
        /// </summary>
        /// <remarks>
        ///   Here we use a mostly vanilla instance, where we customize only the serializer with a <see cref="BinarySerializer"/>.
        /// </remarks>
        public static PersistentCache Persistent { get; set; } = new PersistentCache(new PersistentCacheSettings(), serializer: new BinarySerializer(new BinarySerializerSettings
        {
            AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
            FilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full,
            TypeFormat = System.Runtime.Serialization.Formatters.FormatterTypeStyle.TypesWhenNeeded
        }));

        /// <summary>
        ///   The default instance for <see cref="VolatileCache"/>.
        /// </summary>
        /// <remarks>
        ///   Here we use a mostly vanilla instance, where we customize only the serializer with a <see cref="BinarySerializer"/>.
        /// </remarks>
        public static VolatileCache Volatile { get; set; } = new VolatileCache(new VolatileCacheSettings(), serializer: new BinarySerializer(new BinarySerializerSettings
        {
            AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
            FilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full,
            TypeFormat = System.Runtime.Serialization.Formatters.FormatterTypeStyle.TypesWhenNeeded
        }));
    }
}
