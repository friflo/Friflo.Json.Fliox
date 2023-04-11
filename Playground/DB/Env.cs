using System;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Playground.Client;

namespace Friflo.Playground.DB
{
    internal static class Env
    {
        public const string  File   = "file";
        public const string  Memory = "memory";
        public const string  Cosmos = "cosmos";
            
        private static  FlioxHub _memoryHub;
        private static  FlioxHub _fileHub;
        
        private static readonly string TestDbFolder = CommonUtils.GetBasePath() + "assets~/DB/test_db";
            
        internal static EntityDatabase CreateMemoryDatabase(EntityDatabase sourceDB) {
            var memoryDB = new MemoryDatabase("memory_db") { Schema = sourceDB.Schema};
            memoryDB.SeedDatabase(sourceDB).Wait();
            return memoryDB;
        }
                
        internal static EntityDatabase CreateFileDatabase(DatabaseSchema schema) {
            return new FileDatabase("file_db", TestDbFolder) { Schema = schema};;
        }
        
        internal static void Setup() {
            var typeSchema          = NativeTypeSchema.Create(typeof(TestClient)); // optional - create TypeSchema from Type 
            var databaseSchema      = new DatabaseSchema(typeSchema);
            _fileHub    = new FlioxHub(CreateFileDatabase(databaseSchema));
            _memoryHub  = new FlioxHub(CreateMemoryDatabase(_fileHub.database));
        }

        internal static FlioxHub GetDatabaseHub(string db) {
            switch (db) {
                case Memory:    return _memoryHub;
                case File:      return _fileHub;
            }
            throw new InvalidOperationException($"invalid database Env: {db}");
        }
    }
}