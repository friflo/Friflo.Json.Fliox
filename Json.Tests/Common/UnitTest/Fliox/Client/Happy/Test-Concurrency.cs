// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Remote.Transport.Udp;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        /// <summary>
        /// Assert that the used <see cref="FileDatabase"/> support multi threaded access when reading and writing
        /// the same entity. By using <see cref="AsyncReaderWriterLock"/> a multi read / single write lock
        /// is used for each <see cref="FileContainer"/>. Without this lock it would result in:
        /// IOException: The process cannot access the file 'path' because it is being used by another process. 
        /// </summary>
        [Test] public static void TestConcurrentFileAccessSync () {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var database     = new FileDatabase("test_db", CommonUtils.GetBasePath() + "assets~/DB/test_concurrency_db"))
                    using (var hub          = new FlioxHub(database, TestGlobals.Shared))
                    {
                        await ConcurrentAccess(hub, 2, 2, 10, true);
                    }
                });
            }
        }
        
        public static async Task ConcurrentAccess(FlioxHub hub, int readerCount, int writerCount, int requestCount, bool singleEntity) {
            // --- prepare
            var env         = new SharedEnv();
            var store       = new SimpleStore(hub) { ClientId = "prepare"};
            var entities    = new List<SimplyEntity>();
            int max         = Math.Max(readerCount, writerCount);
            if (singleEntity) {
                var entity = new SimplyEntity{ id = 222, text = "Concurrent accessed entity" };
                for (int n = 0; n < max; n++) {
                    entities.Add(entity);
                }
                store.entities.Upsert(entity);
            } else {
                // use individual entity per readerStores / writerStore
                for (int n = 0; n < max; n++) {
                    entities.Add(new SimplyEntity{ id = n, text = "Concurrent accessed entity" });
                }
                store.entities.UpsertRange(entities);
            }
            await store.SyncTasks();

            var readerStores = new List<SimpleStore>();
            var writerStores = new List<SimpleStore>();
            try {
                for (int n = 0; n < readerCount; n++) {
                    readerStores.Add(new SimpleStore(hub) { ClientId = $"reader-{n}"});
                }
                for (int n = 0; n < writerCount; n++) {
                    writerStores.Add(new SimpleStore(hub) { ClientId = $"writer-{n}" });
                }

                // --- run readers and writers
                var tasks = new List<Task>();

                for (int n = 0; n < readerStores.Count; n++) {
                    tasks.Add(ReadLoop  (readerStores[n], entities[n], requestCount));
                }
                for (int n = 0; n < writerStores.Count; n++) {
                    tasks.Add(WriteLoop (writerStores[n], entities[n], requestCount));
                }
                var lastCount = 0;
                var count = new Thread(() => {
                    while (true) {
                        try {
                            Thread.Sleep(1000);
                        } catch { break; }
                        lastCount = CountRequests(readerStores, writerStores, lastCount);
                    }
                });
                count.Start();

                await Task.WhenAll(tasks);
                CountRequests(readerStores, writerStores, lastCount);
                count.Interrupt();
            }
            finally {
                store.Dispose();
                foreach (var readerStore in readerStores)
                    readerStore.Dispose();
                foreach (var writerStore in writerStores) {
                    writerStore.Dispose();
                }
                env.Dispose();
            }
            
        }

        private static Task ReadLoop (SimpleStore store, SimplyEntity entity, int requestCount) {
            var id = entity.id;
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    var readEntity = store.entities.Find(id);
                    await store.SyncTasks();
                    if (readEntity.Result != null) {
                        continue;
                    }
                    throw new TestException($"Expect entity not null");
                }
            });
        }
        
        private static Task WriteLoop (SimpleStore store, SimplyEntity entity, int requestCount) {
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    store.entities.Upsert(entity);
                    await store.SyncTasks();
                }
            });
        }
        
        private static int CountRequests (List<SimpleStore> readers, List<SimpleStore> writers, int lastCount) {
            int sum = 0;
            sum += CountClientRequests(readers);
            sum += CountClientRequests(writers);
            var requests = sum - lastCount;
            Console.WriteLine($"requests: {requests}, sum: {sum}");
            return sum;
        }
        
        private static int CountClientRequests (List<SimpleStore> stores) {
            int sum = 0; // count without LINQ Sum() to avoid heap allocation noise 
            foreach (var store in stores) { sum += store.GetSyncCount(); }
            return sum;
        }
        
        [Test] public static void   TestConcurrentUdpSync()         { SingleThreadSynchronizationContext.Run(TestConcurrentUdpSyncAsync); }
        
        private static async Task   TestConcurrentUdpSyncAsync () {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared))
            using (var server           = new UdpServerSync("127.0.0.1:5000", hub))
            using (var remoteHub        = new UdpSocketSyncClientHub(TestGlobals.DB, "127.0.0.1:5000", 0, TestGlobals.Shared)) {
                await RunServer(server, async () => {
                    // await remoteHub.Connect();
                    await RunConcurrentHub(remoteHub, 4, 10); // 10 requests are sufficient to force concurrency error
                    await remoteHub.Close();
                });
            }
        }

        // Fixed: run successful in Unity but closing Editor afterwards will hang forever 
        [Test] public static void   TestConcurrentUdp()         { SingleThreadSynchronizationContext.Run(TestConcurrentUdpAsync); }
        
        private static async Task   TestConcurrentUdpAsync () {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared))
            using (var server           = new UdpServer("127.0.0.1:5000", hub))
            using (var remoteHub        = new UdpSocketClientHub(TestGlobals.DB, "127.0.0.1:5000", 0, TestGlobals.Shared)) {
                await RunServer(server, async () => {
                    // await remoteHub.Connect();
                    await RunConcurrentHub(remoteHub, 4, 10); // 10 requests are sufficient to force concurrency error
                    await remoteHub.Close();
                });
            }
        }

        [Test] public static void  TestConcurrentWebSocket()    { SingleThreadSynchronizationContext.Run(TestConcurrentWebSocketAsync); }
        
        /// <summary>
        /// Assert that <see cref="WebSocketClientHub"/> support being used by multiple clients aka
        /// <see cref="FlioxClient"/>'s and using concurrent requests.
        /// All <see cref="FlioxHub"/> implementations support this behavior, so <see cref="WebSocketClientHub"/>
        /// have to ensure this also. It utilize <see cref="ProtocolRequest.reqId"/> to ensure this.
        /// </summary>
        private static async Task TestConcurrentWebSocketAsync () {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared))
            using (var httpHost         = new HttpHost(hub, "/"))
            using (var server           = new HttpServer("http://+:8080/", httpHost))
            using (var remoteHub        = new WebSocketClientHub(TestGlobals.DB, "ws://localhost:8080/", TestGlobals.Shared)) {
                await RunServer(server, async () => {
                    // await remoteHub.Connect();
                    await RunConcurrentHub(remoteHub, 4, 10); // 10 requests are sufficient to force concurrency error
                    await remoteHub.Close();
                });
            }
        }
        
        private static async Task RunConcurrentHub(FlioxHub hub, int clientCount, int requestCount)
        {
            // --- prepare
            var clients = new List<FlioxClient>();
            try {
                for (int n = 0; n < clientCount; n++) {
                    clients.Add(new FlioxClient(hub) { ClientId = $"reader-{n}"});
                }
                var tasks = new List<Task>();
                
                // run loops
                for (int n = 0; n < clients.Count; n++) {
                    tasks.Add(MessageLoop  (clients[n], $"message-{n}", requestCount));
                }
                await Task.WhenAll(tasks);
            }
            finally {
                foreach (var store in clients) {
                    store.Dispose();
                }
            }
        }
        
        private static Task MessageLoop (FlioxClient client, string text, int requestCount) {
            var result = new JsonValue( $"\"{text}\"");
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    var message = client.std.Echo(text);
                    await client.SyncTasks();
                    if (!result.IsEqual(message.RawResult))
                        throw new TestException($"Expect result: {result}, was: {message.RawResult}");
                }
            });
        }
    }
    
    public class SimpleStore : FlioxClient
    {
        public readonly EntitySet <int, SimplyEntity>   entities;
        
        public SimpleStore(FlioxHub hub) : base (hub) { }
    }
    
    // ------------------------------ models ------------------------------
    public class SimplyEntity {
        public  int     id;
        public  string  text;
    }
}