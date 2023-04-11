using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Playground.Client;

#if !UNITY_5_3_OR_NEWER
    using Friflo.Json.Fliox.Hub.Cosmos;
    using Friflo.Playground.CosmosDB;
#endif

namespace Friflo.Playground.DB
{
    internal static class Env
    {
        public const string  File   = "file";
        public const string  Memory = "memory";
        public const string  Cosmos = "cosmos";
            
        private static  FlioxHub        _memoryHub;
        private static  FlioxHub        _fileHub;
        private static  FlioxHub        _cosmosHub;
        
        private static readonly string TestDbFolder = CommonUtils.GetBasePath() + "assets~/DB/test_db";
            
        internal static async Task<EntityDatabase> CreateMemoryDatabase(EntityDatabase sourceDB) {
            var memoryDB = new MemoryDatabase("memory_db") { Schema = sourceDB.Schema };
            await memoryDB.SeedDatabase(sourceDB);
            return memoryDB;
        }
        
        internal static async Task<EntityDatabase> CreateCosmosDatabase(EntityDatabase sourceDB) {
#if !UNITY_5_3_OR_NEWER
            var client              = TestCosmosDB.CreateCosmosClient();
            var createDatabase      = await client.CreateDatabaseIfNotExistsAsync("cosmos_db");
            var cosmosDatabase      = new CosmosDatabase("cosmos_db", createDatabase)
                { Throughput = 400, Schema = sourceDB.Schema };
            await cosmosDatabase.SeedDatabase(sourceDB);
            return cosmosDatabase;
#else
            return null;
#endif
        }
                
        internal static EntityDatabase CreateFileDatabase(DatabaseSchema schema) {
            return new FileDatabase("file_db", TestDbFolder) { Schema = schema };
        }
        
        internal static void Setup() {
            var typeSchema      = NativeTypeSchema.Create(typeof(TestClient)); // optional - create TypeSchema from Type 
            var databaseSchema  = new DatabaseSchema(typeSchema);
            _fileHub            = new FlioxHub(CreateFileDatabase(databaseSchema));
        }

        internal static async Task<FlioxHub> GetDatabaseHub(string db) {
            switch (db) {
                case Memory:    return _memoryHub   ??= new FlioxHub(await CreateMemoryDatabase(_fileHub.database));
                case File:      return _fileHub;
                case Cosmos:    return _cosmosHub   ??= new FlioxHub(await CreateCosmosDatabase(_fileHub.database));
            }
            throw new InvalidOperationException($"invalid database Env: {db}");
        }
    }
}