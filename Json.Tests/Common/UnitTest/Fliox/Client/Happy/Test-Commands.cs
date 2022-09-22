// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
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
        [Test] public async Task TestCommandsSchema()   { await InitDatabaseSchema  (async (store, handler) => await AssertCommandsSchema (store)); }
        [Test] public async Task TestCommands()         { await InitDatabase        (async (store, handler) => await AssertCommands       (store, handler)); }
        
        private static async Task InitDatabaseSchema(Func<PocStore, PocHandler, Task> test) {
            var messageHandler      = new PocHandler();
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new MemoryDatabase(TestGlobals.DB, messageHandler))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var store        = new PocStore(hub) { UserId = "createStore"}) {
                var nativeSchema = NativeTypeSchema.Create(typeof(PocStore));
                database.Schema  = new DatabaseSchema(nativeSchema); 
                await test(store, messageHandler);
            }
        }
        
        private static async Task InitDatabase(Func<PocStore, PocHandler, Task> test) {
            var messageHandler      = new PocHandler();
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new MemoryDatabase(TestGlobals.DB, messageHandler))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var store        = new PocStore(hub) { UserId = "createStore"}) {
                await test(store, messageHandler);
            }
        }
        
        private static async Task AssertCommandsSchema(PocStore store) {
            var containers      = store.std.Containers();
            var commands        = store.std.Messages();
            var schema          = store.std.Schema();
            var dbList          = store.std.Cluster();
            await store.SyncTasks();
            
            var containersResult = containers.Result;
            AreEqual(7,                 containersResult.containers.Length);
            AreEqual("in-memory",       containersResult.storage);
            
            var schemaResult = schema.Result;
            AreEqual(10,                schemaResult.jsonSchemas.Count);
            AreEqual("PocStore",        schemaResult.schemaName);
            AreEqual("Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json", schemaResult.schemaPath);
            
            var dbListResult = dbList.Result;
            AreEqual(1,                 dbListResult.databases.Count);
            var database0 = dbListResult.databases[0];
            AreEqual(7,                 database0.containers.Length);
            AreEqual("in-memory",       database0.storage);
            
            var commandsResult = commands.Result;
            AreEqual(19,                commandsResult.commands.Length);
            AreEqual(5,                 commandsResult.messages.Length);
        }
        
        private static async Task AssertCommands(PocStore store, PocHandler handler) {
            store.articles.Create(new Article { id = "test"});
            await store.SyncTasks();
            var message1        = store.Message1("test message1");
            var message2        = store.test.Message2("test message2");
            var messageAsync    = store.AsyncMessage("async message param");

            var command1        = store.Command1();
            var command2        = store.test.Command2();
            var commandHello    = store.test.CommandHello("hello");
            var containers      = store.std.Containers();
            var commands        = store.std.Messages();
            var schema          = store.std.Schema();
            var dbList          = store.std.Cluster();
            var stdHost         = store.std.Host(new HostParam { memory = true });
            
            await store.SyncTasks();
            
            IsTrue(message1.Success);
            IsTrue(message2.Success);
            IsTrue(messageAsync.Success);
            AreEqual("async message param", handler.manual.AsyncMessageParam);
            
            AreEqual("test message1",   command1.Result);
            AreEqual("test message2",   command2.Result);
            
            AreEqual("hello",           commandHello.Result);

            var containersResult = containers.Result;
            AreEqual(1,                 containersResult.containers.Length);
            AreEqual("in-memory",       containersResult.storage);
            
            var schemaResult = schema.Result;
            IsNull(                     schemaResult);
            
            var dbListResult = dbList.Result;
            AreEqual(1,                 dbListResult.databases.Count);
            var database0 = dbListResult.databases[0];
            AreEqual(1,                 database0.containers.Length);
            AreEqual("in-memory",       database0.storage);
            
            var commandsResult = commands.Result;
            AreEqual(19,                commandsResult.commands.Length);
            AreEqual(3,                 commandsResult.messages.Length);
            //
            var hostResult = stdHost.Result;
            AreEqual("0.0.0",           hostResult.flioxVersion);
            AreEqual("1.0.0",           hostResult.hostVersion);
            AreEqual("host",            hostResult.hostName);
            NotNull(hostResult.memory);
            NotNull(hostResult.routes);
        }
    }
}