using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Cosmos;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.MySQL;
using Friflo.Json.Fliox.Hub.SQLite;
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
        
        internal static             bool    IsCosmosDB  => TEST_DB_PROVIDER == "cosmos";
        internal static             bool    IsMySQL     => TEST_DB_PROVIDER == "mysql";
        internal static             bool    IsMariaDB   => TEST_DB_PROVIDER == "mariadb";

        static Env() {
            TEST_DB_PROVIDER = Environment.GetEnvironmentVariable("TEST_DB_PROVIDER");
            Console.WriteLine($"------------------- TEST_DB_PROVIDER={TEST_DB_PROVIDER} -------------------");
        }
        
        internal static readonly string TestDbFolder = CommonUtils.GetBasePath() + "assets~/DB/test_db";
            
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
        
        internal static async Task<TestClient> GetClient(string db, bool seed = true) {
            if (!hubs.TryGetValue(db, out var hub)) {
                var database =  await CreateDatabase(db).ConfigureAwait(false);
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
        private static async Task<EntityDatabase> CreateDatabase(string db)
        {
            switch (db) {
                case memory_db:
                    return new MemoryDatabase("memory_db");
                case sqlite_db:
                    return CreateSQLiteDatabase("sqlite_db", CommonUtils.GetBasePath() + "sqlite_db.sqlite3");
                case test_db:
                    if (TEST_DB_PROVIDER is null or "file") {
                        return SeedSource;
                    }
                    return await CreateTestDatabase("test_db", TEST_DB_PROVIDER).ConfigureAwait(false);
            }
            throw new InvalidOperationException($"invalid database Env: {db}");
        }
        
        internal static async Task<EntityDatabase> CreateTestDatabase(string db, string provider) {
            switch (provider) {
                case "cosmos":  return await CreateCosmosDatabase(db).ConfigureAwait(false);
                case "sqlite":  return CreateSQLiteDatabase(db, CommonUtils.GetBasePath() + "test_db.sqlite3");
                case "mysql":
                case "mariadb": return await CreateMySQLDatabase(db, provider).ConfigureAwait(false);
            }
            return null;
            // throw new ArgumentException($"invalid provider: {provider}");
        }
        
        private static async Task<EntityDatabase> CreateCosmosDatabase(string db) {
            var client          = CosmosEnv.CreateCosmosClient();
            var createDatabase  = await client.CreateDatabaseIfNotExistsAsync(db).ConfigureAwait(false);
            return new CosmosDatabase(db, createDatabase) { Throughput = 400 };
        }
        
        private static EntityDatabase CreateSQLiteDatabase(string db, string path) {
            return new SQLiteDatabase(db, path);
        }
        
        private static async Task<EntityDatabase> CreateMySQLDatabase(string db, string provider) {

            var connection = await MySQLEnv.OpenMySQLConnection(provider).ConfigureAwait(false);
            /* using var command = new MySqlCommand("SHOW VARIABLES LIKE 'version';", connection);
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false)) {
                var value = reader.GetValue(0);
                Console.WriteLine($"MySQL version: {value}");
            }*/
            // await MySQLUtils.OpenOrCreateDatabase(connection, db).ConfigureAwait(false);
            return new MySQLDatabase(db, connection);
        }
#endif
    }
}

