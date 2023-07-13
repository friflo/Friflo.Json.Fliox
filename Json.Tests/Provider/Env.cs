using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Cosmos;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.MySQL;
using Friflo.Json.Fliox.Hub.PostgreSQL;
using Friflo.Json.Fliox.Hub.Redis;
using Friflo.Json.Fliox.Hub.SQLite;
using Friflo.Json.Fliox.Hub.SQLServer;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Provider.Client;


// ReSharper disable InconsistentNaming
namespace Friflo.Json.Tests.Provider
{
    internal static class Env
    {
        /// <summary>Used for unit tests to check reference behavior</summary>
        internal const string       memory_db  = "memory_db";
        /// <summary>
        /// Used for unit tests to check behavior a specific database implementation.<br/>
        /// The specific database implementation is set by the environment variable: <c>TEST_DB_PROVIDER</c><br/>
        /// See README.md
        /// </summary>
        internal const string       test_db    = "test_db";
        
        internal const string       sqlite_db  = "sqlite_db";

        /// <summary>The source database used to seed test databases</summary>
        private  static             EntityDatabase                  _seedSource;
        /// <summary>
        /// A cache of hubs - one for each database type: <see cref="memory_db"/>, <see cref="sqlite_db"/> and <see cref="test_db"/>
        /// </summary>
        private  static  readonly   Dictionary<string, FlioxHub>    hubs = new Dictionary<string, FlioxHub>();
        /// <summary> contains seeded databases </summary>
        private  static  readonly   HashSet<string>                 seededDatabases = new HashSet<string>();
        
        internal static  readonly   string  TEST_DB_PROVIDER;
        
        private  static bool    IsProvider  (string db1, string db2) => TEST_DB_PROVIDER == db1 || TEST_DB_PROVIDER == db2;
        
        internal static bool    IsCosmosDB  (string db) => TEST_DB_PROVIDER == "cosmos"             && db == test_db;
        internal static bool    IsMySQL     (string db) => IsProvider("mysql",     "mysql_rel")     && db == test_db;
        internal static bool    IsMariaDB   (string db) => IsProvider("mariadb",   "mariadb_rel")   && db == test_db;
        internal static bool    IsSQLServer (string db) => IsProvider("sqlserver", "sqlserver_rel") && db == test_db;
        internal static bool    IsPostgres  (string db) => TEST_DB_PROVIDER == "postgres"           && db == test_db;
        internal static bool    IsSQLite    (string db) => TEST_DB_PROVIDER == "sqlite" || db == sqlite_db;
        private  static bool    IsFileSystem            => TEST_DB_PROVIDER == "file"   || TEST_DB_PROVIDER == null;
        
        private  static readonly    string  SQLiteFile  = $"Data Source={CommonUtils.GetBasePath() + "test_db.sqlite3"}";

        static Env() {
            TEST_DB_PROVIDER = Environment.GetEnvironmentVariable(nameof(TEST_DB_PROVIDER));
            Console.WriteLine($"------------------- {nameof(TEST_DB_PROVIDER)}={TEST_DB_PROVIDER} -------------------");
        }
        
        internal static readonly string TestDbFolder = CommonUtils.GetBasePath() + "assets~/DB/test_db";
        
        internal static void LogSQL(string sql) {
            if (sql != null) {
                // Console.Write($"SQL: {sql}");
            }
        }
        
        private static EntityDatabase SeedSource { get {
            if (_seedSource != null) {
                return _seedSource;
            }
            var databaseSchema  = DatabaseSchema.Create<TestClient>();
            return _seedSource = new FileDatabase("file_db", TestDbFolder, databaseSchema);
        } }
        
        internal static async Task<TestClient> GetClient(string db, bool seed = true)
        {
            if (!hubs.TryGetValue(db, out var hub)) {
                var schema      = SeedSource.Schema;
                var database    = CreateDatabase(db, schema);
                await database.SetupDatabaseAsync().ConfigureAwait(false);
                hub = new FlioxHub(database);
                hubs.Add(db, hub);
            }
            if (seed && !seededDatabases.Contains(db)) {
                seededDatabases.Add(db);
                var source = SeedSource;
                // file system database is the seed source for all other database. So it is not cleared
                if (!IsFileSystem) {
                    await hub.database.ClearDatabase().ConfigureAwait(false);
                }
                await hub.database.SeedDatabase(source).ConfigureAwait(false);
            }
            return new TestClient(hub);
        }

#if UNITY_5_3_OR_NEWER
        private static EntityDatabase CreateDatabase(string db, DatabaseSchema schema) => null;
#else
        private static EntityDatabase CreateDatabase(string db, DatabaseSchema schema)
        {
            switch (db) {
                case memory_db:
                    return new MemoryDatabase("memory_db", schema);
                case sqlite_db:
                    var connection  = $"Data Source={CommonUtils.GetBasePath() + "sqlite_db.sqlite3"}";
                    return new SQLiteDatabase("sqlite_db", connection, schema) {
                        Synchronous = true // Synchronous to simplify debugging
                    };
                case test_db:
                    if (IsFileSystem) {
                        return SeedSource;
                    }
                    return CreateTestDatabase("test_db", TEST_DB_PROVIDER, schema);
                default:
                    return CreateTestDatabase("test_db", db, schema);
            }
        }
        
        /// <summary>
        /// suffix <b>_mc</b> is uses <see cref="TableType.Relational"/> 
        /// </summary>
        private static EntityDatabase CreateTestDatabase(string db, string provider, DatabaseSchema schema)
        {
            var connection = EnvConfig.GetConnectionString(provider);
            switch (provider) {
                case "sqlite":          return new SQLiteDatabase       (db, SQLiteFile, schema) { TableType = TableType.JsonColumn };
                case "mysql":           return new MySQLDatabase        (db, connection, schema) { TableType = TableType.JsonColumn };
                case "mysql_rel":       return new MySQLDatabase        (db, connection, schema);
                case "mariadb":         return new MariaDBDatabase      (db, connection, schema) { TableType = TableType.JsonColumn };
                case "mariadb_rel":     return new MariaDBDatabase      (db, connection, schema);
                case "postgres":        return new PostgreSQLDatabase   (db, connection, schema) { TableType = TableType.JsonColumn };
                case "postgres_rel":    return new PostgreSQLDatabase   (db, connection, schema);
                case "sqlserver":       return new SQLServerDatabase    (db, connection, schema) { TableType = TableType.JsonColumn };
                case "sqlserver_rel":   return new SQLServerDatabase    (db, connection, schema);
                case "redis":           return new RedisHashDatabase    (db, connection, schema);
                case "cosmos":          return new CosmosDatabase       (db, connection, schema);
            }
            throw new ArgumentException($"invalid provider: {provider}");
        }
#endif
    }
}
