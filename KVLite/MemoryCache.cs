using PommaLabs.KVLite.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Common;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using System.Runtime.CompilerServices;

namespace PommaLabs.KVLite
{
    class MemoryCache : AbstractCache<MemoryCacheSettings>
    {
        public override IClock Clock
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ICompressor Compressor
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ILog Log
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ISerializer Serializer
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override MemoryCacheSettings Settings
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override void AddInternal<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval)
        {
            
        }

        protected override void ClearInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate)
        {
            throw new NotImplementedException();
        }

        protected override bool ContainsInternal(string partition, string key)
        {
            throw new NotImplementedException();
        }

        protected override long CountInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.ConsiderExpiryDate)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetFormattingMembers()
        {
            throw new NotImplementedException();
        }

        protected override Option<TVal> GetInternal<TVal>(string partition, string key)
        {
            throw new NotImplementedException();
        }

        protected override Option<CacheItem<TVal>> GetItemInternal<TVal>(string partition, string key)
        {
            throw new NotImplementedException();
        }

        protected override CacheItem<TVal>[] GetItemsInternal<TVal>(string partition)
        {
            throw new NotImplementedException();
        }

        protected override Option<TVal> PeekInternal<TVal>(string partition, string key)
        {
            throw new NotImplementedException();
        }

        protected override Option<CacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key)
        {
            throw new NotImplementedException();
        }

        protected override CacheItem<TVal>[] PeekItemsInternal<TVal>(string partition)
        {
            throw new NotImplementedException();
        }

        protected override void RemoveInternal(string partition, string key)
        {
            throw new NotImplementedException();
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static string BuildCacheKey(string partition, string key)
        {
            return $"<p>{partition}</p><k>{key}</k>";
        }
    }
}
