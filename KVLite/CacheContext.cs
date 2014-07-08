using System.Data.Common;
using System.Data.SQLite;
using DbExtensions;

namespace KVLite
{
    internal sealed class CacheContext : Database
    {
        private CacheContext(DbConnection connection) : base(connection)
        {
        }

        public static CacheContext Create(string connectionString)
        {
            var cnn = new SQLiteConnection(connectionString);
            cnn.Open();
            return new CacheContext(cnn);
        }
    }
}
