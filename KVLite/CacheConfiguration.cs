using System.Data.Entity;
using System.Data.Entity.SqlServerCompact;

namespace KVLite
{
    public sealed class CacheConfiguration : DbConfiguration
    {
        public CacheConfiguration()
        {
            SetProviderServices(SqlCeProviderServices.ProviderInvariantName, SqlCeProviderServices.Instance);
        }
    }
}