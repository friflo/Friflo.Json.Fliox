// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.DB.Threading;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestStore
    {
        /// <summary>
        /// Assert that the used <see cref="FileDatabase"/> support multi threaded access when reading and writing
        /// the same entity. By using <see cref="AsyncReaderWriterLock"/> a multi read / single write lock
        /// is used for each <see cref="FileContainer"/>. Without this lock it would result in:
        /// IOException: The process cannot access the file 'path' because it is being used by another process. 
        /// </summary>
        [Test] public static void TestConcurrentFileAccessSync () {
            using (var _                = UtilsInternal.SharedPools) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var database     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/testConcurrencyDb"))
                    using (var hub          = new DatabaseHub(database))
                    {
                        await ConcurrentAccess(hub, 2, 2, 10, true);
                    }
                });
            }
        }
        
        public static async Task ConcurrentAccess(DatabaseHub hub, int readerCount, int writerCount, int requestCount, bool singleEntity) {
            // --- prepare
            var typeStore   = new TypeStore();
            var store       = new SimpleStore(hub, typeStore, "prepare");
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
            await store.ExecuteTasksAsync();

            var readerStores = new List<SimpleStore>();
            var writerStores = new List<SimpleStore>();
            try {
                for (int n = 0; n < readerCount; n++) {
                    readerStores.Add(new SimpleStore(hub, typeStore, $"reader-{n}"));
                }
                for (int n = 0; n < writerCount; n++) {
                    writerStores.Add(new SimpleStore(hub, typeStore, $"writer-{n}"));
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
                typeStore.Dispose();
            }
            
        }

        private static Task ReadLoop (SimpleStore store, SimplyEntity entity, int requestCount) {
            var id = entity.id;
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    var readEntities = store.entities.Read();
                    readEntities.Find(id);
                    await store.ExecuteTasksAsync();
                    if (1 !=  readEntities.Results.Count)
                        throw new TestException($"Expect entities Count: 1. was: {readEntities.Results.Count}");
                    if (!readEntities.Results.ContainsKey(id))
                        throw new TestException($"Expect entity with id: {id}");
                }
            });
        }
        
        private static Task WriteLoop (SimpleStore store, SimplyEntity entity, int requestCount) {
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    store.entities.Upsert(entity);
                    await store.ExecuteTasksAsync();
                }
            });
        }
        
        private static int CountRequests (List<SimpleStore> readers, List<SimpleStore> writers, int lastCount) {
            int sum = 0;
            sum += readers.Sum(reader => reader.GetSyncCount());
            sum += writers.Sum(writer => writer.GetSyncCount());
            var requests = sum - lastCount;
            Console.WriteLine($"requests: {requests}, sum: {sum}");
            return sum;
        }
        
        /// <summary>
        /// Assert that <see cref="WebSocketClientHub"/> support being used by multiple clients aka
        /// <see cref="FlioxClient"/>'s and using concurrent requests.
        /// All <see cref="DatabaseHub"/> implementations support this behavior, so <see cref="WebSocketClientHub"/>
        /// have to ensure this also. It utilize <see cref="ProtocolRequest.reqId"/> to ensure this.
        /// </summary>
#if !UNITY_5_3_OR_NEWER 
        [Test]
        public static async Task TestConcurrentWebSocket () {
            using (var _                = UtilsInternal.SharedPools) // for LeakTestsFixture
            using (var database         = new MemoryDatabase())
            using (var hub          	= new DatabaseHub(database))
            using (var hostDatabase     = new HttpHostHub(hub))
            using (var server           = new HttpListenerHost("http://+:8080/", hostDatabase))
            using (var remoteDatabase   = new WebSocketClientHub("ws://localhost:8080/")) {
                await RunServer(server, async () => {
                    await remoteDatabase.Connect();
                    await ConcurrentWebSocket(remoteDatabase, 4, 10); // 10 requests are sufficient to force concurrency error
                    await remoteDatabase.Close();
                });
            }
        }
#endif
        
        private static async Task ConcurrentWebSocket(DatabaseHub hub, int clientCount, int requestCount)
        {
            // --- prepare
            var clients = new List<FlioxClient>();
            try {
                var typeStore = new TypeStore();
                for (int n = 0; n < clientCount; n++) {
                    clients.Add(new FlioxClient(hub, typeStore, null, $"reader-{n}"));
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
            var result = new JsonUtf8( $"\"{text}\"");
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    var message = client.SendMessage(StdMessage.Echo, text);
                    await client.ExecuteTasksAsync();
                    if (!result.IsEqual(message.ResultJson))
                        throw new TestException($"Expect result: {result}, was: {message.ResultJson}");
                }
            });
        }
    }
    
    public class SimpleStore : FlioxClient
    {
        public readonly EntitySet <int, SimplyEntity>   entities;
        
        public SimpleStore(DatabaseHub hub, TypeStore typeStore, string clientId) : base (hub, typeStore, null, clientId) {}
    }
    
    // ------------------------------ models ------------------------------
    public class SimplyEntity {
        public  int     id;
        public  string  text;
    }
}