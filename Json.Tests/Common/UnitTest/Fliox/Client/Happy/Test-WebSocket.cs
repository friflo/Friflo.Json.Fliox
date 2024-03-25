// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Threading;
using NUnit.Framework;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
#else
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
#if !UNITY_5_3_OR_NEWER
        [Test]      public void  WebSocketConnectSync()       { SingleThreadSynchronizationContext.Run(WebSocketConnect); }
        
        private static async Task WebSocketConnect() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(EventDispatching.Send))
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared))
            using (var httpHost         = new HttpHost(hub, "/", TestGlobals.Shared))
            using (var server           = new HttpServer("http://+:8080/", httpHost))
            using (var remoteHub        = new WebSocketClientHub(TestGlobals.DB, "ws://localhost:8080/", TestGlobals.Shared)) {
                hub.AddExtensionDB (new MonitorDB("monitor", hub));
                hub.EventDispatcher = eventDispatcher;
                await RunServer(server, async () => {
                    await AutoConnect(remoteHub);
                    await remoteHub.Close();
                });
                await eventDispatcher.StopDispatcher();
            }
        }
        
        ///<summary>
        /// Note-1:
        /// test multiple concurrent <see cref="FlioxClient.SyncTasks"/> calls create/use the same <see cref="WebSocketConnection"/>
        /// </summary> 
        private static async Task AutoConnect (FlioxHub hub) {
            using (var client   = new PocStore(hub)                 { UserId = "AutoConnect"})
            using (var monitor  = new MonitorStore(hub, "monitor")  { UserId = "AutoConnect"}){
                IsNull(client.ClientId); // client don't assign a client id
                var clientResult = client.std.Client(new ClientParam { queueEvents = true });
                // Note-1
                var sync1 = client.SyncTasks();
                var sync2 = client.SyncTasks();
                var sync3 = client.SyncTasks();
                await Task.WhenAll(sync1, sync2, sync3);
                
                AreEqual(0, clientResult.Result.queuedEvents);  // no events send right now
                
                var findClient1 = monitor.clients.Read().Find(new ShortString("1"));
                await monitor.SyncTasks();
                
                var client1 = findClient1.Result;
                NotNull(client1.subscriptionEvents);
                var eventDelivery = client1.subscriptionEvents.Value;
                AreEqual(0, eventDelivery.seq);     // no events send right now
                AreEqual(0, eventDelivery.queued);  // no events send right now
                IsTrue(eventDelivery.queueEvents);  // is set by std.Client - queueEvents above
                IsTrue(eventDelivery.connected);
                
                client.std.Echo("111");
                client.std.Echo("222");
                client.SubscribeMessage("*", (message, context) => { });
                await client.SyncTasks();

                AreEqual("1", client.ClientId); // client id assigned by Hub as client instruct a subscription
            }
        }
#endif
        
        [Test]      public void  WebSocketConnectErrorSync()       { SingleThreadSynchronizationContext.Run(WebSocketConnectError); }
        private static async Task WebSocketConnectError() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var remoteHub        = new WebSocketClientHub(TestGlobals.DB, "ws://0.0.0.0:8080/", TestGlobals.Shared)) {
                await AutoConnectError(remoteHub);
                await remoteHub.Close();
            }
        }
        
        private static async Task AutoConnectError (FlioxHub hub) {
            using (var client = new PocStore(hub) { UserId = "AutoConnectError", ClientId = "error-client"}) {
                var syncError = await client.TrySyncTasks();
                NotNull(syncError.Failed);
                StringAssert.StartsWith("Internal WebSocketException: Unable to connect to the remote server", syncError.Message);
                
                var sync1 = client.SyncTasks();
                var sync2 = client.SyncTasks();
                var sync3 = client.SyncTasks();
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