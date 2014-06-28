using System.Data.Entity;
using System.Data.SqlServerCe;

namespace KVLite
{
    internal sealed class CacheContext : DbContext
    {
        public CacheContext(string connectionString) : base(new SqlCeConnection(connectionString), true)
        {
            // Empty, for now
        }

        public DbSet<CacheItem> CacheItems { get; set; }
    }
}
