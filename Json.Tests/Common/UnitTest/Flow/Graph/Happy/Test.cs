// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Database.Remote;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using UnityEngine.TestTools;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
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
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var database     = new MemoryDatabase())
            using (var createStore  = await TestRelationPoC.CreateStore(database))
            using (var useStore     = new PocStore(database, "useStore"))  {
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator FileCreateCoroutine() { yield return RunAsync.Await(FileCreate(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  FileCreateAsync() { await FileCreate(); }

        private static async Task FileCreate() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var createStore  = await TestRelationPoC.CreateStore(fileDatabase))
            using (var useStore     = new PocStore(fileDatabase, "useStore")) {
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator FileUseCoroutine() { yield return RunAsync.Await(FileUse()); }
        [Test]      public async Task  FileUseAsync() { await FileUse(); }
        
        private static async Task FileUse() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var createStore  = await TestRelationPoC.CreateStore(fileDatabase))
            using (var useStore     = new PocStore(fileDatabase, "useStore")) {
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator HttpCreateCoroutine() { yield return RunAsync.Await(HttpCreate()); }
        [Test]      public async Task  HttpCreateAsync() { await HttpCreate(); }
        
        private static async Task HttpCreate() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var hostDatabase     = new HttpHostDatabase(fileDatabase, "http://+:8080/", null))
            using (var remoteDatabase   = new HttpClientDatabase("http://localhost:8080/")) {
                await RunRemoteHost(hostDatabase, async () => {
                    using (var createStore      = await TestRelationPoC.CreateStore(remoteDatabase))
                    using (var useStore         = new PocStore(remoteDatabase, "useStore")) {
                        await TestStores(createStore, useStore);
                    }
                });
            }
        }
        
        // accepting WebSockets in Unity fails at IsWebSocketRequest. See: 
        // [Help Wanted - Websocket Server in Standalone build - Unity Forum] https://forum.unity.com/threads/websocket-server-in-standalone-build.1072526/
        // [UnityTest] public IEnumerator WebSocketCreateCoroutine()   { yield return RunAsync.Await(WebSocketCreate()); }
        [Test]      public async Task  WebSocketCreateAsync()       { await WebSocketCreate(); }
        
        private static async Task WebSocketCreate() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var eventBroker      = new EventBroker())
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var hostDatabase     = new HttpHostDatabase(fileDatabase, "http://+:8080/", null))
            using (var remoteDatabase   = new WebSocketClientDatabase("ws://localhost:8080/"))
            using (var listenDb         = new PocStore(remoteDatabase, "listenDb")) {
                fileDatabase.eventBroker = eventBroker;
                await RunRemoteHost(hostDatabase, async () => {
                    await remoteDatabase.Connect();
                    var pocListener             = await CreatePocListener(listenDb);
                    using (var createStore      = await TestRelationPoC.CreateStore(remoteDatabase))
                    using (var useStore         = new PocStore(remoteDatabase, "useStore")) {
                        await TestStores(createStore, useStore);
                    }
                    pocListener.AssertCreateStoreChanges();
                    await remoteDatabase.Close();
                });
            }
        }
        
        [UnityTest] public IEnumerator LoopbackUseCoroutine() { yield return RunAsync.Await(LoopbackUse()); }
        [Test]      public async Task  LoopbackUseAsync() { await LoopbackUse(); }
        
        private static async Task LoopbackUse() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var loopbackDatabase = new LoopbackDatabase(fileDatabase)) {
                using (var createStore      = new PocStore(loopbackDatabase, "createStore"))
                using (var useStore         = new PocStore(loopbackDatabase, "useStore")) {
                    await TestStores(createStore, useStore);
                }
            }
        }
        
        internal static async Task RunRemoteHost(HttpHostDatabase remoteHost, Func<Task> run) {
            remoteHost.Start();
            Task hostTask = null;
            try {
                hostTask = Task.Run(() => {
                    // await hostDatabase.HandleIncomingConnections();
                    remoteHost.Run();
                    // await Task.Delay(100); // test awaiting hostTask
                    Logger.Info("1. RemoteHost finished");
                });
                
                await run();
            } finally {
                await remoteHost.Stop();
                if (hostTask != null)
                    await hostTask;
                Logger.Info("2. awaited hostTask");
            }
        } 

        // ------------------------------------ test assertion methods ------------------------------------
        private static async Task TestStores(PocStore createStore, PocStore useStore) {
            await AssertWriteRead       (createStore);
            await AssertEntityIdentity  (createStore);
            await AssertQuery           (createStore);
            await AssertRead            (createStore);
            await AssertRefAssignment   (useStore);
        }
        

        private static async Task TestCreate(Func<PocStore, Task> test) {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var createStore  = await TestRelationPoC.CreateStore(fileDatabase)) {
                await test(createStore);
            }
        }
        
        private static async Task TestUse(Func<PocStore, Task> test) {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var createStore  = new PocStore(fileDatabase, "createStore")) {
                await test(createStore);
            }
        }
    }
}