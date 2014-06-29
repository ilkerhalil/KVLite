using System.Data;
using System.Data.SQLite;

namespace KVLite
{
    internal static class CacheContext
    {
        public static IDbConnection Create(string connectionString)
        {
            var cnn = new SQLiteConnection(connectionString);
            cnn.Open();
            return cnn;
        }
    }
}
