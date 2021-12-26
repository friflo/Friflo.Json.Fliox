// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Schema.Native;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestStore
    {
        [Test] public async Task TestCommandsSchema()   { await InitDatabaseSchema  (async (store) => await AssertCommandsSchema (store)); }
        [Test] public async Task TestCommands()         { await InitDatabase        (async (store) => await AssertCommands       (store)); }
        
        private static async Task InitDatabaseSchema(Func<PocStore, Task> test) {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new MemoryDatabase(new PocHandler()))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var store        = new PocStore(hub) { UserId = "createStore"})
            using (var nativeSchema = new NativeTypeSchema(typeof(PocStore)))
            using (database.Schema  = new DatabaseSchema(nativeSchema)) {
                await test(store);
            }
        }
        
        private static async Task InitDatabase(Func<PocStore, Task> test) {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new MemoryDatabase(new PocHandler()))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var store        = new PocStore(hub) { UserId = "createStore"}) {
                await test(store);
            }
        }
        
        private static async Task AssertCommandsSchema(PocStore store) {
            var containers      = store.DbContainers();
            var commands        = store.DbCommands();
            var catalogSchema   = store.DbSchema();
            var dbList          = store.DbList();
            await store.SyncTasks();
            
            var containersResult = containers.Result;
            AreEqual(6,                 containersResult.containers.Length);
            AreEqual("MemoryDatabase",  containersResult.databaseType);
            
            var schemaResult = catalogSchema.Result;
            AreEqual(9,                 schemaResult.jsonSchemas.Count);
            AreEqual("PocStore",        schemaResult.schemaName);
            AreEqual("Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json", schemaResult.schemaPath);
            
            var dbListResult = dbList.Result;
            AreEqual(1,                 dbListResult.databases.Count);
            var catalog0 = dbListResult.databases[0];
            AreEqual(6,                 catalog0.containers.Length);
            AreEqual("MemoryDatabase",  catalog0.databaseType);
            
            var commandsResult = commands.Result;
            AreEqual(7,                 commandsResult.commands.Length);
        }
        
        private static async Task AssertCommands(PocStore store) {
            store.articles.Create(new Article { id = "test"});
            await store.SyncTasks();
            
            var containers       = store.DbContainers();
            var commands        = store.DbCommands();
            var catalogSchema   = store.DbSchema();
            var dbList          = store.DbList();
            await store.SyncTasks();
            
            var containersResult = containers.Result;
            AreEqual(1,                 containersResult.containers.Length);
            AreEqual("MemoryDatabase",  containersResult.databaseType);
            
            var schemaResult = catalogSchema.Result;
            IsNull(                     schemaResult);
            
            var dbListResult = dbList.Result;
            AreEqual(1,                 dbListResult.databases.Count);
            var catalog0 = dbListResult.databases[0];
            AreEqual(1,                 catalog0.containers.Length);
            AreEqual("MemoryDatabase",  catalog0.databaseType);
            
            var commandsResult = commands.Result;
            AreEqual(7,                 commandsResult.commands.Length);
        }
    }
}