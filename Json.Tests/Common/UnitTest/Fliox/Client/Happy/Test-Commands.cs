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
    public partial class TestHappy
    {
        [Test] public async Task TestCommandsSchema()   { await InitDatabaseSchema  (async (store) => await AssertCommandsSchema (store)); }
        [Test] public async Task TestCommands()         { await InitDatabase        (async (store) => await AssertCommands       (store)); }
        
        private static async Task InitDatabaseSchema(Func<PocStore, Task> test) {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new MemoryDatabase(new PocHandler()))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var store        = new PocStore(hub) { UserId = "createStore"})
            using (var nativeSchema = new NativeTypeSchema(typeof(PocStore))) {
                database.Schema  = new DatabaseSchema(nativeSchema); 
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
            var containers      = store.std.Containers();
            var commands        = store.std.Messages();
            var catalogSchema   = store.std.Schema();
            var dbList          = store.std.Cluster();
            await store.SyncTasks();
            
            var containersResult = containers.Result;
            AreEqual(6,                 containersResult.containers.Length);
            AreEqual("memory",          containersResult.storage);
            
            var schemaResult = catalogSchema.Result;
            AreEqual(9,                 schemaResult.jsonSchemas.Count);
            AreEqual("PocStore",        schemaResult.schemaName);
            AreEqual("Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json", schemaResult.schemaPath);
            
            var dbListResult = dbList.Result;
            AreEqual(1,                 dbListResult.databases.Count);
            var catalog0 = dbListResult.databases[0];
            AreEqual(6,                 catalog0.containers.Length);
            AreEqual("memory",          catalog0.storage);
            
            var commandsResult = commands.Result;
            AreEqual(13,                commandsResult.commands.Length);
            AreEqual(3,                 commandsResult.messages.Length);
        }
        
        private static async Task AssertCommands(PocStore store) {
            store.articles.Create(new Article { id = "test"});
            await store.SyncTasks();
            var message1        = store.Message1("test message1");
            var message2        = store.test.Message2("test message2");
            var messageAsync    = store.AsyncMessage("foo");

            var command1        = store.Command1();
            var command2        = store.test.Command2();
            var commandHello    = store.test.CommandHello("hello");
            var containers      = store.std.Containers();
            var commands        = store.std.Messages();
            var catalogSchema   = store.std.Schema();
            var dbList          = store.std.Cluster();
            
            await store.SyncTasks();
            
            IsTrue(message1.Success);
            IsTrue(message2.Success);
            IsTrue(messageAsync.Success);
            
            AreEqual("test message1",   command1.Result);
            AreEqual("test message2",   command2.Result);
            
            AreEqual("hello",           commandHello.Result);

            var containersResult = containers.Result;
            AreEqual(1,                 containersResult.containers.Length);
            AreEqual("memory",          containersResult.storage);
            
            var schemaResult = catalogSchema.Result;
            IsNull(                     schemaResult);
            
            var dbListResult = dbList.Result;
            AreEqual(1,                 dbListResult.databases.Count);
            var catalog0 = dbListResult.databases[0];
            AreEqual(1,                 catalog0.containers.Length);
            AreEqual("memory",          catalog0.storage);
            
            var commandsResult = commands.Result;
            AreEqual(13,                commandsResult.commands.Length);
            AreEqual(3,                 commandsResult.messages.Length);
        }
    }
}