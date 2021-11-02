// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Fliox.Hub.Protocol;
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
    public partial class TestStore : LeakTestsFixture
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
            using (var _            = HostGlobal.Pool) // for LeakTestsFixture
            using (var database     = new MemoryDatabase(new PocHandler()))
            using (var hub          = new FlioxHub(database))
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
            using (var _            = HostGlobal.Pool) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.PocStoreFolder, new PocHandler()))
            using (var hub          = new FlioxHub(database))
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
            using (var _            = HostGlobal.Pool) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var hub          = new FlioxHub(database))
            using (var useStore     = new PocStore(hub) { UserId = "useStore"}) {
                await TestStores(useStore, useStore);
                
                useStore.Reset();   AssertReset(useStore);
                
                await TestStores(useStore, useStore);
            }
        }
        

        
        [UnityTest] public IEnumerator HttpCreateCoroutine() { yield return RunAsync.Await(HttpCreate()); }
        [Test]      public async Task  HttpCreateAsync() { await HttpCreate(); }
        
        private static async Task HttpCreate() {
            using (var _                = HostGlobal.Pool) // for LeakTestsFixture
            using (var database         = new FileDatabase(TestGlobals.PocStoreFolder, new PocHandler()))
            using (var hub          	= new FlioxHub(database))
            using (var hostHub          = new HttpHostHub(hub))
            using (var server           = new HttpListenerHost("http://+:8080/", hostHub))
            using (var remoteDatabase   = new HttpClientHub("http://localhost:8080/")) {
                await RunServer(server, async () => {
                    using (var createStore      = new PocStore(remoteDatabase) { UserId = "createStore", ClientId = "create-client"})
                    using (var useStore         = new PocStore(remoteDatabase) { UserId = "useStore",    ClientId = "use-client"}) {
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
            using (var _                = HostGlobal.Pool) // for LeakTestsFixture
            using (var eventBroker      = new EventBroker(false))
            using (var database         = new FileDatabase(TestGlobals.PocStoreFolder, new PocHandler()))
            using (var hub          	= new FlioxHub(database))
            using (var hostHub          = new HttpHostHub(hub))
            using (var server           = new HttpListenerHost("http://+:8080/", hostHub))
            using (var remoteHub        = new WebSocketClientHub("ws://localhost:8080/"))
            using (var listenDb         = new PocStore(remoteHub) { UserId = "listenDb", ClientId = "listen-client"}) {
                hub.EventBroker = eventBroker;
                await RunServer(server, async () => {
                    await remoteHub.Connect();
                    var listenProcessor     = await CreateSubscriptionProcessor(listenDb, EventAssertion.Changes);
                    using (var createStore  = new PocStore(remoteHub) { UserId = "createStore", ClientId = "create-client"})
                    using (var useStore     = new PocStore(remoteHub) { UserId = "useStore",    ClientId = "use-client"}) {
                        var createSubscriber = await CreateSubscriptionProcessor(createStore, EventAssertion.NoChanges);
                        await TestRelationPoC.CreateStore(createStore);
                        
                        while (!listenProcessor.receivedAll ) { await Task.Delay(1); }
                        
                        AreEqual(1, createSubscriber.EventSequence);  // received no change events for changes done by itself
                        listenProcessor.AssertCreateStoreChanges();
                        await TestStores(createStore, useStore);
                    }
                    await remoteHub.Close();
                });
                await eventBroker.FinishQueues();
            }
        }
        
        [Test]      public void  WebSocketReconnectSync()       { SingleThreadSynchronizationContext.Run(WebSocketReconnect); }
        
        /// Test WebSocket disconnect while having changes subscribed. Change events pushed by the database may not arrived at subscriber.
        /// To ensure all change events arrive at <see cref="SubscriptionProcessor"/> <see cref="SyncRequest.eventAck"/>
        /// is used to inform database about arrived events. All not acknowledged events are resent.
        private static async Task WebSocketReconnect() {
            using (var _                = HostGlobal.Pool) // for LeakTestsFixture
            using (var eventBroker      = new EventBroker(true))
            using (var database         = new FileDatabase(TestGlobals.PocStoreFolder, new PocHandler()))
            using (var hub          	= new FlioxHub(database))
            using (var hostHub          = new HttpHostHub(hub))
            using (var server           = new HttpListenerHost("http://+:8080/", hostHub))
            using (var remoteHub        = new WebSocketClientHub("ws://localhost:8080/"))
            using (var listenDb         = new PocStore(remoteHub) { UserId = "listenDb", ClientId = "listen-client"}) {
                hostHub.fakeOpenClosedSockets = true;
                hub.EventBroker = eventBroker;
                await RunServer(server, async () => {
                    await remoteHub.Connect();
                    var listenProcessor    = await CreateSubscriptionProcessor(listenDb, EventAssertion.Changes);
                    using (var createStore  = new PocStore(hub) { UserId = "createStore", ClientId = "create-client"}) {
                        await remoteHub.Close();
                        // all change events sent by createStore doesnt arrive at listenDb
                        await TestRelationPoC.CreateStore(createStore);
                        AreEqual(0, listenProcessor.EventSequence);
                        
                        // subscriber contains send events which are not acknowledged
                        IsTrue(eventBroker.NotAcknowledgedEvents() > 0);

                        await remoteHub.Connect();
                        
                        AreEqual(0, listenDb.Tasks.Count);
                        await listenDb.SyncTasks();  // an empty SyncTasks() is sufficient initiate re-sending all not-received change events

                        while (!listenProcessor.receivedAll ) { await Task.Delay(1); }
                        
                        listenProcessor.AssertCreateStoreChanges();

                        await listenDb.SyncTasks();  // all changes are received => state of store remains unchanged
                        
                        // subscriber contains NO send events which are not acknowledged
                        AreEqual(0, eventBroker.NotAcknowledgedEvents());

                        listenProcessor.AssertCreateStoreChanges();
                    }
                    await remoteHub.Close();
                });
                await eventBroker.FinishQueues();
            }
        }
        
        [UnityTest] public IEnumerator  LoopbackUseCoroutine() { yield return RunAsync.Await(LoopbackUse()); }
        [Test]      public void         LoopbackUseSync()     { SingleThreadSynchronizationContext.Run(LoopbackUse); }
        
        private static async Task LoopbackUse() {
            using (var _                = HostGlobal.Pool) // for LeakTestsFixture
            using (var eventBroker      = new EventBroker(false))
            using (var database         = new FileDatabase(TestGlobals.PocStoreFolder, new PocHandler()))
            using (var hub          	= new FlioxHub(database))
            using (var loopbackHub      = new LoopbackHub(hub))
            using (var listenDb         = new PocStore(loopbackHub) { UserId = "listenDb", ClientId = "listen-client"}) {
                loopbackHub.host.EventBroker    = eventBroker;
                var listenProcessor         = await CreateSubscriptionProcessor(listenDb, EventAssertion.Changes);
                using (var createStore      = new PocStore(loopbackHub) { UserId = "createStore", ClientId = "create-client"})
                using (var useStore         = new PocStore(loopbackHub) { UserId = "useStore",    ClientId = "use-client"}) {
                    var createSubscriber        = await CreateSubscriptionProcessor(createStore, EventAssertion.NoChanges);
                    await TestRelationPoC.CreateStore(createStore);
                    
                    while (!listenProcessor.receivedAll ) { await Task.Delay(1); }
                    
                    AreEqual(1, createSubscriber.EventSequence);  // received no change events for changes done by itself
                    listenProcessor.AssertCreateStoreChanges();
                    await TestStores(createStore, useStore);
                }
                await eventBroker.FinishQueues();
            }
        }
        
        internal static async Task RunServer(HttpListenerHost server, Func<Task> run) {
            server.Start();
            Task hostTask = null;
            try {
                hostTask = Task.Run(() => {
                    // await hostDatabase.HandleIncomingConnections();
                    server.Run();
                    // await Task.Delay(100); // test awaiting hostTask
                    Logger.Info("1. RemoteHost finished");
                });
                
                await run();
            } finally {
                await server.Stop();
                if (hostTask != null)
                    await hostTask;
                Logger.Info("2. awaited hostTask");
            }
        } 

        // ------------------------------------ test assertion methods ------------------------------------
        public static async Task TestStores(PocStore createStore, PocStore useStore) {
            await AssertRefAssignment   (useStore);
            await AssertWriteRead       (createStore);
            await AssertEntityIdentity  (createStore);
            await AssertQuery           (createStore);
            await AssertRead            (createStore);
        }
        
        private static void AssertReset(FlioxClient client) {
            IsNull(client.UserId);
            IsNull(client.ClientId);
            IsNull(client.Token);
            AreEqual(0, client.Tasks.Count);
            AreEqual(0, client.StoreInfo.peers);
            AreEqual(0, client.GetSyncCount());
        }

        private static async Task TestCreate(Func<PocStore, Task> test) {
            using (var _            = HostGlobal.Pool) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.PocStoreFolder, new PocHandler()))
            using (var hub          = new FlioxHub(database))
            using (var createStore  = new PocStore(hub) { UserId = "createStore"}) {
                await TestRelationPoC.CreateStore(createStore);
                await test(createStore);
            }
        }
        
        private static async Task TestUse(Func<PocStore, Task> test) {
            using (var _            = HostGlobal.Pool) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var hub          = new FlioxHub(database))
            using (var createStore  = new PocStore(hub) { UserId = "createStore"}) {
                await test(createStore);
            }
        }
    }
}