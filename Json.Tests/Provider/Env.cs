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
        
        internal static             bool    IsCosmosDB          => TEST_DB_PROVIDER == "cosmos";
        internal static             bool    IsMySQL             => TEST_DB_PROVIDER == "mysql";
        internal static             bool    IsMariaDB           => TEST_DB_PROVIDER == "mariadb";
        internal static             bool    IsPostgres          => TEST_DB_PROVIDER == "postgres";
        internal static             bool    IsSQLServer         => TEST_DB_PROVIDER == "sqlserver";
        internal static             bool    IsSQLite(string db) => TEST_DB_PROVIDER == "sqlite" || db == sqlite_db;
        private  static             bool    IsFileSystem        => TEST_DB_PROVIDER == "file";

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
            
        internal static async Task Seed(EntityDatabase target, EntityDatabase source) {
            target.Schema = source.Schema;
            await target.SeedDatabase(source).ConfigureAwait(false);
        }
        
        private static EntityDatabase SeedSource { get {
            if (_seedSource != null) {
                return _seedSource;
            }
            var databaseSchema  = new DatabaseSchema(typeof(TestClient));
            return _seedSource = new FileDatabase("file_db", TestDbFolder) { Schema = databaseSchema };
        } }
        
        internal static async Task<TestClient> GetClient(string db, bool seed = true)
        {
            if (!hubs.TryGetValue(db, out var hub)) {
                var database    = CreateDatabase(db);
                database.Schema = SeedSource.Schema;
                hub = new FlioxHub(database);
                hubs.Add(db, hub);
            }
            if (seed && !seededDatabases.Contains(db)) {
                seededDatabases.Add(db);
                var source = SeedSource;
                await Seed(hub.database, source).ConfigureAwait(false);
            }
            return new TestClient(hub);
        }

#if UNITY_5_3_OR_NEWER
        private static Task<EntityDatabase> CreateDatabase(string db) => null;
#else
        private static EntityDatabase CreateDatabase(string db)
        {
            switch (db) {
                case memory_db:
                    return new MemoryDatabase("memory_db");
                case sqlite_db:
                    return CreateSQLiteDatabase("sqlite_db", CommonUtils.GetBasePath() + "sqlite_db.sqlite3");
                case test_db:
                    if (TEST_DB_PROVIDER is null || IsFileSystem) {
                        return SeedSource;
                    }
                    return CreateTestDatabase("test_db", TEST_DB_PROVIDER);
                default:
                    return CreateTestDatabase("test_db", db);
            }
            throw new InvalidOperationException($"invalid database Env: {db}");
        }
        
        internal static EntityDatabase CreateTestDatabase(string db, string provider)
        {
            switch (provider) {
                case "cosmos":      return CreateCosmosDatabase(db);
                case "sqlite":      return CreateSQLiteDatabase(db, CommonUtils.GetBasePath() + "test_db.sqlite3");
                case "mysql":
                case "mariadb":     return CreateMySQLDatabase      (db, provider);
                case "postgres":    return CreatePostgresDatabase   (db);
                case "sqlserver":   return CreateSQLServerDatabase  (db);
                case "redis":       return CreateRedisDatabase      (db);
            }
            return null;
            // throw new ArgumentException($"invalid provider: {provider}");
        }
        
        private static EntityDatabase CreateCosmosDatabase(string db)
        {
            var client = EnvConfig.CreateCosmosClient();
            return new CosmosDatabase(db, client, db) { Throughput = 400 };
        }
        
        private static EntityDatabase CreateSQLiteDatabase(string db, string path)
        {
            return new SQLiteDatabase(db, path);
        }
        
        private static EntityDatabase CreateMySQLDatabase(string db, string provider)
        {
            var connection = EnvConfig.GetConnectionString(provider);
            switch (provider) {
                case "mysql":   return new MySQLDatabase  (db, connection);
                case "mariadb": return new MariaDBDatabase(db, connection);
                default:        throw new ArgumentException($"invalid MySQL provider: {provider}");
            }
        }
        
        private static EntityDatabase CreatePostgresDatabase(string db)
        {
            var connection = EnvConfig.GetConnectionString("postgres");
            return new PostgreSQLDatabase(db, connection);
        }
        
        private static EntityDatabase CreateSQLServerDatabase(string db)
        {
            var connection = EnvConfig.GetConnectionString("sqlserver");
            return new SQLServerDatabase(db, connection);
        }
        
        private static EntityDatabase CreateRedisDatabase(string db)
        {
            var connection = EnvConfig.GetConnectionString("redis");
            return new RedisDatabase(db, connection);
        }
#endif
    }
}

