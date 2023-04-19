using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.DB.Client;

#if !UNITY_5_3_OR_NEWER
    using Friflo.Json.Fliox.Hub.Cosmos;
#endif

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Tests.DB
{
    public static class Env
    {
        /// <summary>Used for unit tests to check reference behavior</summary>
        public const string  memory_db  = "memory_db";
        /// <summary>
        /// Used for unit tests to check behavior a specific database implementation.<br/>
        /// The specific database implementation is set by the environment variable: <c>TEST_DB</c><br/>
        /// See README.md
        /// </summary>
        public const string  test_db    = "test_db";
            
        private  static             FlioxHub    _memoryHub;
        private  static             FlioxHub    _fileHub;
        private  static             FlioxHub    _testHub;
        internal static  readonly   string      TEST_DB = Environment.GetEnvironmentVariable("TEST_DB");
        
        private static readonly string TestDbFolder = CommonUtils.GetBasePath() + "assets~/DB/test_db";
            
        public static async Task<EntityDatabase> CreateMemoryDatabase(EntityDatabase sourceDB) {
            var memoryDB = new MemoryDatabase("memory_db") { Schema = sourceDB.Schema };
            await memoryDB.SeedDatabase(sourceDB);
            return memoryDB;
        }
        
        public static async Task<EntityDatabase> CreateCosmosDatabase(EntityDatabase sourceDB) {
#if !UNITY_5_3_OR_NEWER
            var client              = EnvCosmosDB.CreateCosmosClient();
            var createDatabase      = await client.CreateDatabaseIfNotExistsAsync("test_db");
            var cosmosDatabase      = new CosmosDatabase("test_db", createDatabase)
                { Throughput = 400, Schema = sourceDB.Schema };
            await cosmosDatabase.SeedDatabase(sourceDB);
            return cosmosDatabase;
#else
            return null;
#endif
        }
                
        public static EntityDatabase CreateFileDatabase(DatabaseSchema schema) {
            return new FileDatabase("file_db", TestDbFolder) { Schema = schema };
        }
        
        internal static void Setup() {
            var typeSchema      = NativeTypeSchema.Create(typeof(TestClient)); // optional - create TypeSchema from Type 
            var databaseSchema  = new DatabaseSchema(typeSchema);
            _fileHub            = new FlioxHub(CreateFileDatabase(databaseSchema));
        }

        internal static async Task<FlioxHub> GetDatabaseHub(string db) {
            switch (db) {
                case memory_db:
                    return _memoryHub   ??= new FlioxHub(await CreateMemoryDatabase(_fileHub.database));
                case test_db:
                    if (TEST_DB is null or "file") {
                        return _fileHub;
                    }
                    if (TEST_DB == "cosmos") {
                        return _testHub   ??= new FlioxHub(await CreateCosmosDatabase(_fileHub.database));
                    }
                    throw new InvalidOperationException($"invalid TEST_DB: {TEST_DB}");
            }
            throw new InvalidOperationException($"invalid database Env: {db}");
        }
    }
}