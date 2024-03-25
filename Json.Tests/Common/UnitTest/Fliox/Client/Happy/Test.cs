// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy : LeakTestsFixture
    {
        /// withdraw from allocation detection by <see cref="LeakTestsFixture"/> => init before tracking starts
        [NUnit.Framework.OneTimeSetUp]    public static void  Init()       { TestGlobals.Init(); }
        [NUnit.Framework.OneTimeTearDown] public static void  Dispose()    { TestGlobals.Dispose(); }
        

        [UnityTest] public IEnumerator  CollectAwaitCoroutine() { yield return RunAsync.Await(CollectAwait(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task   CollectAwaitAsync() { await CollectAwait(); }
        
        private static async Task CollectAwait() {
            List<Task> tasks = new List<Task>();
            for (int n = 0; n < 1000; n++) {
                Task task = Task.Delay(1);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }

        [UnityTest] public IEnumerator  ChainAwaitCoroutine() { yield return RunAsync.Await(ChainAwait(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task   ChainAwaitAsync() { await ChainAwait(); }
        private static async Task ChainAwait() {
            for (int n = 0; n < 5; n++) {
                await Task.Delay(1);
            }
        }
        
        [UnityTest] public IEnumerator  MemoryCreateCoroutine() { yield return RunAsync.Await(MemoryCreate()); }
        [Test]      public async Task   MemoryCreateAsync() { await MemoryCreate(); }
        
        private static async Task MemoryCreate() {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new MemoryDatabase(TestGlobals.DB, null, new PocService()))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var createStore  = new PocStore(hub) { UserId = "createStore"})
            using (var useStore     = new PocStore(hub) { UserId = "useStore"})  {
                await TestRelationPoC.CreateStore(createStore);
                await TestStores(createStore, useStore);
                
                // Make client state changes and add some arbitrary sync tasks to test Reset()
                createStore.UserId      = "some UserId";
                createStore.ClientId    = "some ClientId";
                createStore.Token       = "some Token";
                createStore.articles.QueryAll();
                createStore.articles.Delete("xxx");
                useStore.articles.QueryAll();
                useStore.articles.Delete("yyy");
                createStore.Reset();    AssertReset(createStore);
                useStore.Reset();       AssertReset(useStore);
                
                await TestRelationPoC.CreateStore(createStore);
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator FileCreateCoroutine() { yield return RunAsync.Await(FileCreate(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  FileCreateAsync() { await FileCreate(); }

        private static async Task FileCreate() {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder, null, new PocService()))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var createStore  = new PocStore(hub) { UserId = "createStore"})
            using (var useStore     = new PocStore(hub) { UserId = "useStore"}) {
                await TestRelationPoC.CreateStore(createStore);
                await TestStores(createStore, useStore);
                
                createStore.Reset();    AssertReset(createStore);
                useStore.Reset();       AssertReset(useStore);
                
                await TestRelationPoC.CreateStore(createStore);
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator FileUseCoroutine() { yield return RunAsync.Await(FileUse()); }
        [Test]      public async Task  FileUseAsync() { await FileUse(); }
        
        private static async Task FileUse() {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var useStore     = new PocStore(hub) { UserId = "useStore"}) {
                await TestStores(useStore, useStore);
                
                useStore.Reset();   AssertReset(useStore);
                
                await TestStores(useStore, useStore);
            }
        }
        
        [Test]
        public static void TestHttpInfo() {
            var database    = new MemoryDatabase("test");
            var hub         = new FlioxHub(database);
            var host        = new HttpHost(hub, "/"); // set hub feature: HttpInfo 
            var routes      = host.Routes;
            // HttpHost standard routes
            AreEqual("/rest",       routes[0]);
            AreEqual("/schema",     routes[1]);
        }


        [UnityTest] public IEnumerator HttpCreateCoroutine() { yield return RunAsync.Await(HttpCreate()); }
        [Test]      public async Task  HttpCreateAsync() { await HttpCreate(); }
        
        private static async Task HttpCreate() {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder, null, new PocService()))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var httpHost     = new HttpHost(hub, "/"))
            using (var server       = new HttpServer("http://+:8080/", httpHost))
            using (var remoteHub    = new HttpClientHub(TestGlobals.DB, "http://localhost:8080/", TestGlobals.Shared)) {
                await RunServer(server, async () => {
                    
                    using (var createStore      = new PocStore(remoteHub) { UserId = "createStore", ClientId = "create-client"})
                    using (var useStore         = new PocStore(remoteHub) { UserId = "useStore",    ClientId = "use-client"}) {
                        await TestRelationPoC.CreateStore(createStore);
                        await TestStores(createStore, useStore);
                        
                        createStore.Reset();    AssertReset(createStore);
                        useStore.Reset();       AssertReset(useStore);
                        
                        await TestRelationPoC.CreateStore(createStore);
                        await TestStores(createStore, useStore);
                    }
                });
            }
        }
        
        // accepting WebSockets in Unity fails at IsWebSocketRequest. See: 
        // [Help Wanted - Websocket Server in Standalone build - Unity Forum] https://forum.unity.com/threads/websocket-server-in-standalone-build.1072526/
        // [UnityTest] public IEnumerator WebSocketCreateCoroutine()   { yield return RunAsync.Await(WebSocketCreate()); }
        [Test]      public void  WebSocketCreateSync()       { SingleThreadSynchronizationContext.Run(WebSocketCreate); }
        
        /// This test ensure that a <see cref="WebSocketClientHub"/> behaves exactly like all other
        /// <see cref="FlioxHub"/> implementations in this file.
        /// It also ensures that a single <see cref="WebSocketClientHub"/> instance can be used by multiple clients
        /// simultaneously. In this case three <see cref="PocStore"/> instances.
        private static async Task WebSocketCreate() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(EventDispatching.Send))
            using (var database         = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder, null, new PocService()))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared))
            using (var httpHost         = new HttpHost(hub, "/", TestGlobals.Shared))
            using (var server           = new HttpServer("http://+:8080/", httpHost))
            using (var remoteHub        = new WebSocketClientHub(TestGlobals.DB, "ws://localhost:8080/", TestGlobals.Shared))
            using (var listenDb         = new PocStore(remoteHub) { UserId = "listenDb", ClientId = "listen-client"}) {
                hub.EventDispatcher = eventDispatcher;
                await RunServer(server, async () => {
                    // await remoteHub.Connect();
                    var listenSubscriber    = await CreatePocStoreSubscriber(listenDb, EventAssertion.Changes);
                    using (var createStore  = new PocStore(remoteHub) { UserId = "createStore", ClientId = "create-client"})
                    using (var useStore     = new PocStore(remoteHub) { UserId = "useStore",    ClientId = "use-client"}) {
                        var createSubscriber = await CreatePocStoreSubscriber(createStore, EventAssertion.NoChanges);
                        await TestRelationPoC.CreateStore(createStore);
                        
                        while (!listenSubscriber.receivedAll ) { await Task.Delay(1); }
                        
                        AreEqual(8, createSubscriber.EventCount);
                        IsTrue(createSubscriber.IsOrigin);
                        listenSubscriber.AssertCreateStoreChanges();
                        await TestStores(createStore, useStore);
                    }
                    await remoteHub.Close();
                });
                await eventDispatcher.StopDispatcher();
            }
        }
        
        [Test]      public void  WebSocketReconnectSync()       { SingleThreadSynchronizationContext.Run(WebSocketReconnect); }
        
        /// Test WebSocket disconnect while having changes subscribed. Change events pushed by the database may not arrived at subscriber.
        /// To ensure all change events arrive at <see cref="Friflo.Json.Fliox.Hub.Client.SubscriptionProcessor"/>
        /// <see cref="SyncRequest.eventAck"/> is used to inform database about arrived events. All not acknowledged events are resent.
        private static async Task WebSocketReconnect() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(EventDispatching.QueueSend))
            using (var database         = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder, null, new PocService()))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared))
            using (var hostHub          = new HttpHost(hub, "/"))
            using (var server           = new HttpServer("http://+:8080/", hostHub))
            using (var remoteHub        = new WebSocketClientHub(TestGlobals.DB, "ws://localhost:8080/", TestGlobals.Shared))
            using (var listenDb         = new PocStore(remoteHub) { UserId = "listenDb", ClientId = "listen-client"}) {
                hostHub.hub.GetFeature<RemoteHostEnv>().fakeOpenClosedSockets = true;
                hub.EventDispatcher = eventDispatcher;
                await RunServer(server, async () => {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    // await remoteHub.Connect();
                    var listenSubscriber    = await CreatePocStoreSubscriber(listenDb, EventAssertion.Changes);
                    using (var createStore  = new PocStore(hub) { UserId = "createStore", ClientId = "create-client"}) {
                        await remoteHub.Close();
                        // all change events sent by createStore doesn't arrive at listenDb
                        await TestRelationPoC.CreateStore(createStore);
                        AreEqual(0, listenSubscriber.EventCount);
                        
                        // subscriber contains send events which are not acknowledged
                        IsTrue(eventDispatcher.QueuedEventsCount() > 0);

                        // await remoteHub.Connect();

                        AreEqual(0, listenDb.Tasks.Count);
                        await listenDb.SyncTasks();  // an empty SyncTasks() is sufficient initiate re-sending all not-received change events

                        while (!listenSubscriber.receivedAll) {
                            if (stopWatch.ElapsedMilliseconds  > 2000) throw new InvalidOperationException("WebSocketReconnect timeout");
                            await Task.Delay(1);
                        }
                        listenSubscriber.AssertCreateStoreChanges();

                        await listenDb.SyncTasks();  // all changes are received => state of store remains unchanged
                        
                        // subscriber contains NO send events which are not acknowledged
                        AreEqual(0, eventDispatcher.QueuedEventsCount());

                        listenSubscriber.AssertCreateStoreChanges();
                    }
                    await remoteHub.Close();
                });
                await eventDispatcher.StopDispatcher();
            }
        }
        
        [UnityTest] public IEnumerator  LoopbackUseCoroutine() { yield return RunAsync.Await(LoopbackUse()); }
        [Test]      public void         LoopbackUseSync()     { SingleThreadSynchronizationContext.Run(LoopbackUse); }
        
        private static async Task LoopbackUse() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(EventDispatching.Send))
            using (var database         = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder, null, new PocService()))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared))
            using (var loopbackHub      = new LoopbackHub(hub))
            using (var listenDb         = new PocStore(loopbackHub) { UserId = "listenDb", ClientId = "listen-client"}) {
                loopbackHub.hub.EventDispatcher    = eventDispatcher;
                var listenSubscriber        = await CreatePocStoreSubscriber(listenDb, EventAssertion.Changes);
                using (var createStore      = new PocStore(loopbackHub) { UserId = "createStore", ClientId = "create-client"})
                using (var useStore         = new PocStore(loopbackHub) { UserId = "useStore",    ClientId = "use-client"}) {
                    var createSubscriber = await CreatePocStoreSubscriber(createStore, EventAssertion.NoChanges);
                    await TestRelationPoC.CreateStore(createStore);
                    
                    while (!listenSubscriber.receivedAll ) { await Task.Delay(1); }
                    
                    AreEqual(8, createSubscriber.EventCount);
                    IsTrue(createSubscriber.IsOrigin);
                    listenSubscriber.AssertCreateStoreChanges();
                    await TestStores(createStore, useStore);
                }
                await eventDispatcher.StopDispatcher();
            }
        }
        
        internal static async Task RunServer(IRemoteServer server, Func<Task> run) {
            server.Start();
            Task runTask = null;
            try {
                runTask = Task.Run(() => {
                    server.Run();
                    // await Task.Delay(100); // test awaiting hostTask
                    Logger.Info("1. run() finished");
                });
                
                await run();
            }
            finally {
                // TODO remove Task.Delay(1) - was in HttpServer.Stop()
                await Task.Delay(1).ConfigureAwait(false);
                server.Stop();
                if (runTask != null)
                    await runTask;
                Logger.Info($"2. {server.GetType().Name} stopped");
            }
        } 

        // ------------------------------------ test assertion methods ------------------------------------
        public static async Task TestStores(PocStore createStore, PocStore useStore) {
            AreEqual(TestGlobals.DB, createStore.DatabaseName);
            AreEqual(TestGlobals.DB, useStore.DatabaseName);
            await AssertWriteRead       (createStore);
            await AssertEntityIdentity  (createStore);
            await AssertQuery           (createStore);
            await AssertQueryCursor     (createStore);
            await AssertAggregate       (createStore);
            await AssertRead            (createStore);
        }
        
        private static void AssertReset(FlioxClient client) {
            IsNull(client.UserId);
            IsNull(client.ClientId);
            IsNull(client.Token);
            AreEqual(0, client.Tasks.Count);
            AreEqual(0, client.ClientInfo.peers);
            AreEqual(0, client.GetSyncCount());
        }

        private static async Task TestCreate(Func<PocStore, Task> test) {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder, null, new PocService()))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var createStore  = new PocStore(hub) { UserId = "createStore"}) {
                await TestRelationPoC.CreateStore(createStore);
                await test(createStore);
            }
        }
    }
}