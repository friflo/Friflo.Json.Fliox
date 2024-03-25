// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
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
        
        private static async Task InitDatabaseSchema(Func<SubPocStore, SubPocService, Task> test) {
            var service             = new SubPocService();
            var schema              = DatabaseSchema.Create<SubPocStore>(); 
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new MemoryDatabase(TestGlobals.DB, schema, service))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var store        = new SubPocStore(hub) { UserId = "createStore"}) {
                await test(store, service);
            }
        }
        
        private static async Task InitDatabase(Func<SubPocStore, SubPocService, Task> test) {
            var service         = new SubPocService();
            using (var _        = SharedEnv.Default) // for LeakTestsFixture
            using (var database = new MemoryDatabase(TestGlobals.DB, null, service))
            using (var hub      = new FlioxHub(database, TestGlobals.Shared))
            using (var store    = new SubPocStore(hub) { UserId = "createStore"}) {
                await test(store, service);
            }
        }
        
        private static async Task AssertCommandsSchema(SubPocStore store) {
            var containers      = store.std.Containers();
            var commands        = store.std.Messages();
            var schema          = store.std.Schema();
            var dbList          = store.std.Cluster();
            await store.SyncTasks();
            
            var containersResult = containers.Result;
            AreEqual(9,                 containersResult.containers.Length);
            AreEqual("in-memory",       containersResult.storage);
            
            var schemaResult = schema.Result;
            AreEqual(12,                schemaResult.jsonSchemas.Count);
            AreEqual("SubPocStore",     schemaResult.schemaName);
            AreEqual("Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy.json", schemaResult.schemaPath);
            
            var dbListResult = dbList.Result;
            AreEqual(1,                 dbListResult.databases.Count);
            var database0 = dbListResult.databases[0];
            AreEqual(9,                 database0.containers.Length);
            AreEqual("in-memory",       database0.storage);
            
            var commandsResult = commands.Result;
            AreEqual(28,                commandsResult.commands.Length);
            AreEqual(6,                 commandsResult.messages.Length);
        }
        
        private static async Task AssertCommands(SubPocStore store, SubPocService service) {
            store.articles.Create(new Article { id = "test"});
            await store.SyncTasks();
            var message1        = store.Message1("test message1");
            var message2        = store.test.Message2("test message2");
            var message3        = store.test.AsyncMessage3("test async message3");
            var messageAsync    = store.AsyncMessage("async message param");

            var command1        = store.Command1();
            var commandIntArray = store.CommandIntArray(new int [] { 42 });
            var commandClassArray = store.CommandClassArray(new Article [] { new Article { id = "foo", name = "bar" } });
            var command2        = store.test.Command2();
            var commandHello    = store.test.CommandHello("hello");
            var containers      = store.std.Containers();
            var commands        = store.std.Messages();
            var schema          = store.std.Schema();
            var dbList          = store.std.Cluster();
            var stdHost         = store.std.Host(new HostParam { memory = true });
            var command3        = store.Command3();
            var command4        = store.sub.Command4();
            
            await store.SyncTasks();
            
            IsTrue(message1.Success);
            IsTrue(message2.Success);
            IsTrue(message3.Success);
            IsTrue(messageAsync.Success);
            AreEqual("async message param", service.manual.AsyncMessageParam);
            
            AreEqual("test message1",   command1.Result);
            AreEqual(new int[] { 42 },  commandIntArray.Result);
            
            var articlesResult = commandClassArray.Result;
            AreEqual(1,                 articlesResult.Length);
            AreEqual("foo",             articlesResult[0].id);
            AreEqual("bar",             articlesResult[0].name);
            
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
            AreEqual(28,                commandsResult.commands.Length);
            AreEqual(4,                 commandsResult.messages.Length);
            //
            var hostResult = stdHost.Result;
            // AreEqual("0.0.0",        hostResult.flioxVersion); -> will fail at CD tests
            AreEqual("1.0.0",           hostResult.hostVersion);
            AreEqual("host",            hostResult.hostName);
            NotNull(hostResult.memory);
            IsNull (hostResult.routes);
            
            AreEqual("Command3",        command3.Result);
            AreEqual("Command4",        command4.Result);
        }
    }
    
    class SubPocStore : PocStore
    {
        public readonly SubCommands sub;

        public CommandTask<string>      Command3 ()                 => send.Command<string>          ("sub.Command3");
        
        public SubPocStore(FlioxHub hub, string dbName = null) : base(hub, dbName) {
            sub = new SubCommands(this);
        }
    }
    
    [MessagePrefix("sub.")]
    public sealed class SubCommands : HubMessages
    {
        internal SubCommands(SubPocStore client) : base(client) { }
        
        public CommandTask<string>      Command4 ()                 => send.Command<string>          ();
    }
    
    public class SubPocService : PocService
    {
        [CommandHandler("sub.Command3")]
        public Result<string> Command3(Param<string> param, MessageContext context) {
            return Result.Value("Command3");    // ensue API available
        }
        
        [CommandHandler("sub.Command4")]
        public Result<string> Command4(Param<string> param, MessageContext context) {
            return "Command4";
        }
    }

}