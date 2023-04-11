using System;
using Friflo.Json.Fliox.Hub.Cosmos;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Playground.Client;
using Friflo.Playground.CosmosDB;

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
            
        internal static EntityDatabase CreateMemoryDatabase(EntityDatabase sourceDB) {
            var memoryDB = new MemoryDatabase("memory_db") { Schema = sourceDB.Schema };
            memoryDB.SeedDatabase(sourceDB).Wait();
            return memoryDB;
        }
        
        internal static EntityDatabase CreateCosmosDatabase(EntityDatabase sourceDB) {
            var client              = TestCosmosDB.CreateCosmosClient();
            var createDatabase      = client.CreateDatabaseIfNotExistsAsync("cosmos_db").Result;
            var cosmosDatabase      = new CosmosDatabase("cosmos_db", createDatabase)
                { Throughput = 400, Schema = sourceDB.Schema };
            cosmosDatabase.SeedDatabase(sourceDB).Wait();
            return cosmosDatabase;
        }
                
        internal static EntityDatabase CreateFileDatabase(DatabaseSchema schema) {
            return new FileDatabase("file_db", TestDbFolder) { Schema = schema };
        }
        
        internal static void Setup() {
            var typeSchema      = NativeTypeSchema.Create(typeof(TestClient)); // optional - create TypeSchema from Type 
            var databaseSchema  = new DatabaseSchema(typeSchema);
            _fileHub            = new FlioxHub(CreateFileDatabase(databaseSchema));
        }

        internal static FlioxHub GetDatabaseHub(string db) {
            switch (db) {
                case Memory:    return _memoryHub   ??= new FlioxHub(CreateMemoryDatabase(_fileHub.database));
                case File:      return _fileHub;
                case Cosmos:    return _cosmosHub   ??= new FlioxHub(CreateCosmosDatabase(_fileHub.database));
            }
            throw new InvalidOperationException($"invalid database Env: {db}");
        }
    }
}