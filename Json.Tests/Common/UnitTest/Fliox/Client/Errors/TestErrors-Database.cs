// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Errors
{
    public partial class TestErrors
    {
        [Test]      public async Task  TestHttpDatabaseErrors() { await HttpRemoteDatabaseErrors(); }
        
        private async Task HttpRemoteDatabaseErrors() {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new MemoryDatabase(TestGlobals.DB))
            using (var testHub      = new FlioxHub(database, TestGlobals.Shared))
            using (var httpHost     = new HttpHost(testHub, "/"))
            using (var server       = new HttpServer("http://+:8080/", httpHost)) {
                await Happy.TestHappy.RunServer(server, async () => {
                    using (var remoteHub    = new HttpClientHub("unknown_db", "http://localhost:8080/", TestGlobals.Shared)) {
                        await AssertUnknownDatabase(remoteHub);
                        await AssertUnknownDefaultDatabase(remoteHub);
                    }
                    
                    /* todo
                    using (var remoteHub    = new WebSocketClientHub("unknown_db", "ws://localhost:8080/", TestGlobals.Shared)) {
                        await remoteHub.Connect();
                        await AssertUnknownDatabase(remoteHub);
                        await AssertUnknownDefaultDatabase(remoteHub);
                    } */
                });
            }
        }
        
        [Test]      public async Task  TestLoopbackDatabaseErrors() { await LoopbackDatabaseErrors(); }
        
        private async Task LoopbackDatabaseErrors() {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new MemoryDatabase(TestGlobals.DB))
            using (var testHub      = new FlioxHub(database, TestGlobals.Shared))
            using (var loopbackHub  = new LoopbackHub(testHub)) {
                await AssertUnknownDatabase(loopbackHub);
                // await AssertUnknownDefaultDatabase(loopbackHub); LoopbackHub cannot pass a different dbName
            }
        }
        
        private static async Task AssertUnknownDatabase(FlioxHub hub) {
            using (var store = new PocStore(hub, "unknown_db2") { UserId = "dbError", ClientId = "db-error"}) {
                try {
                    await store.SyncTasks();
                    Fail("Expect exception");
                } catch (Exception e) {
                    IsTrue(e is SyncTasksException);
                    AreEqual("database not found: 'unknown_db2'", e.Message);
                }
            }
        }
        
        private static async Task AssertUnknownDefaultDatabase(FlioxHub hub) {
            using (var store = new PocStore(hub) { UserId = "dbError", ClientId = "db-error"}) {
                try {
                    await store.SyncTasks();
                    Fail("Expect exception");
                } catch (Exception e) {
                    IsTrue(e is SyncTasksException);
                    AreEqual("database not found: 'unknown_db'", e.Message);
                }
            }
        }
    }
}