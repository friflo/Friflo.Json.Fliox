// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Threading;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [Test]      public void  WebSocketConnectSync()       { SingleThreadSynchronizationContext.Run(WebSocketConnect); }
        
        private static async Task WebSocketConnect() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(false))
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared))
            using (var httpHost         = new HttpHost(hub, "/", TestGlobals.Shared))
            using (var server           = new HttpListenerHost("http://+:8080/", httpHost))
            using (var remoteHub        = new WebSocketClientHub(TestGlobals.DB, "ws://localhost:8080/", TestGlobals.Shared)) {
                hub.EventDispatcher = eventDispatcher;
                await RunServer(server, async () => {
                    await AutoConnect(remoteHub);
                    await remoteHub.Close();
                });
                await eventDispatcher.FinishQueues();
            }
        }
        
        private static async Task AutoConnect (FlioxHub hub) {
            using (var remoteClient     = new PocStore(hub) { UserId = "AutoConnect", ClientId = "auto-client"}) {
                var sync1 = remoteClient.SyncTasks();
                var sync2 = remoteClient.SyncTasks();
                var sync3 = remoteClient.SyncTasks();
                await Task.WhenAll(sync1, sync2, sync3);
            }
        }
        
        [Test]      public void  WebSocketConnectErrorSync()       { SingleThreadSynchronizationContext.Run(WebSocketConnectError); }
        private static async Task WebSocketConnectError() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var remoteHub        = new WebSocketClientHub(TestGlobals.DB, "ws://0.0.0.0:8080/", TestGlobals.Shared)) {
                await AutoConnectError(remoteHub);
                await remoteHub.Close();
            }
        }
        
        private static async Task AutoConnectError (FlioxHub hub) {
            using (var remoteClient     = new PocStore(hub) { UserId = "AutoConnectError", ClientId = "error-client"}) {
                var syncError = await remoteClient.TrySyncTasks();
                NotNull(syncError.failed);
                StringAssert.StartsWith("Internal WebSocketException: Unable to connect to the remote server", syncError.Message);
                
                var sync1 = remoteClient.SyncTasks();
                var sync2 = remoteClient.SyncTasks();
                var sync3 = remoteClient.SyncTasks();
                try {
                    await Task.WhenAll(sync1, sync2, sync3);
                    Fail("expect exceptions");
                } catch(Exception e) {
                    StringAssert.StartsWith("Internal WebSocketException: Unable to connect to the remote server", e.Message);
                }
            }
        }
    }
}