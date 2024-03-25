// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Utils;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class TestUtils
    {
        [Test]
        public void TestQueryRef() {
            using (var __       = SharedEnv.Default) // for LeakTestsFixture
            using (var database = new MemoryDatabase("db"))
            using (var env      = new SharedEnv())
            using (var hub      = new FlioxHub(database, env))
            using (var store    = new PocStore(hub) { UserId = "TestQueryRef"}) {
                var orders = store.orders;
                var customerId = orders.Query(o => o.customer == "customer-1");
                AreEqual("QueryTask<Order> (filter: o => o.customer == 'customer-1')", customerId.ToString());
                
                store.SyncTasks().Wait();
            }
        }

        [Test]
        public void TestEmptyDictionary() {
            // ReSharper disable once CollectionNeverUpdated.Local
            var empty = new EmptyDictionary<string, string>();
            empty.Clear(); // no exception
            
            var kvPair = new KeyValuePair<string, string>("A","B");
            IsFalse(empty.Contains(kvPair));
            
            IsFalse(empty.ContainsKey("X"));
            
            AreEqual(0, empty.Count);
            
            IsFalse(empty.TryGetValue("Y", out var value));
            IsNull(value);
            
            foreach (var _ in empty) {
                Fail("cant be reached - dictionary is always empty");
            }
        }
        
        [Test]
        public void TestEntityProcessor() {
            using (var processor = new EntityProcessor()) {
                {
                    // --- return modified JSON
                    var     json = new JsonValue("{\"myId\": \"123\"}");
                    var     result  = processor.ReplaceKey(json, "myId", false, "id", out JsonKey _, out _);
                    AreEqual("{\"id\":\"123\"}", result.AsString());
                } {
                    // --- return modified JSON
                    var     json =  new JsonValue("{\"myId\": \"111\"}");
                    var     result  = processor.ReplaceKey(json, "myId", true, "id", out JsonKey _, out _);
                    AreEqual("{\"id\":111}", result.AsString());
                } {
                    // --- return modified JSON
                    var     json = new JsonValue("{\"id\": 456}");
                    var     result  = processor.ReplaceKey(json, "id", false, "id", out JsonKey _, out _);
                    AreEqual("{\"id\":\"456\"}", result.AsString());
                } {
                    // --- return modified JSON - key ist not first member
                    var     json = new JsonValue("{\"x\":42,\"id2\":222}");
                    var     result  = processor.ReplaceKey(json, "id2", true, "id", out JsonKey _, out _);
                    AreEqual("{\"x\":42,\"id\":222}", result.AsString());
                } {
                    // --- return modified JSON - previous member contains unicode (☀), key is unicode (🌎)
                    var     json = new JsonValue("{\"☀\":1,\"🌎\": \"xyz\",\"♥\":2}");
                    var     result  = processor.ReplaceKey(json, "🌎", false, "🪐", out JsonKey _, out _);
                    AreEqual("{\"☀\":1,\"🪐\":\"xyz\",\"♥\":2}", result.AsString());
                } {
                    // --- return original JSON
                    var     json = new JsonValue("{\"id\": 789}");
                    var     result  = processor.ReplaceKey(json, "id", true, "id", out JsonKey _, out _);
                    IsTrue(json.IsEqualReference(result));
                } {
                    // --- return original JSON
                    var     json =  new JsonValue("{\"id\": \"abc\"}");
                    // null defaults to "id"
                    var result = processor.ReplaceKey(json, null, false, "id", out JsonKey _, out _);
                    IsTrue(json.IsEqualReference(result));
                } {
                    // --- error on invalid integer key. Valid range: [long.MinValue, long.MaxValue] 
                    var     json = new JsonValue("{\"id\": 9223372036854776000}");
                    var     result  = processor.ReplaceKey(json, "id", false, "id", out JsonKey _, out string error);
                    IsTrue(result.IsNull()); 
                    AreEqual("invalid integer key: Value out of range when parsing long: 9223372036854776000", error);
                }
            }
        }

#if !UNITY_2020_1_OR_NEWER
        [Test]
        public void TestReadRelationTypeSafety() {
            var env     = new SharedEnv();
            var hub     = new FlioxHub(new MemoryDatabase("db"), env);
            var store   = new PocStore(hub) { UserId = "TestDictionaryValueIterator"};
            var readArticles = store.articles.Read();
            var relationSelector = store.articles.RelationPath(store.producers, o => o.producer);
            readArticles.ReadRelation(store.producers, relationSelector);
        }
        
        /// <summary>
        /// FlioxClient / sub class resource consumption:
        /// Memory:      ~ 632 + 24 * (EntitySet count) [bytes]
        /// Execution:   ~ 350 + 10 * (EntitySet count) [ns]
        /// Allocations: 6 (independent from EntitySet count)
        /// </summary>
        /*  Allocation list
            Client.Internal.ClientEventReceiver 24 bytes
            Client.Internal.EntitySet[]         80 bytes
            Client.Internal.SyncStore	        40 bytes
            Client.PocStore                    544 bytes
            Dictionary<Task, Host.SyncContext>  80 bytes
            List<Client.SyncFunction>           32 bytes
        */
        [Test]
        public void BenchmarkCreateClient() {
            var env         = new SharedEnv();
            var hub         = new NoopDatabaseHub("noop_db", env);
            var _           = new PocStore(hub);
            var __          = new PocStore(hub);
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int count = 1; // 1_000_000;
            for (int n = 0; n < count; n++) {
                new PocStore(hub);                      // ~ 0.420 µs (Release,best)
            }
            stopwatch.Stop();
            Console.WriteLine($"client instantiation count: {count}, ms: {stopwatch.ElapsedMilliseconds}");
            
            var start = Mem.GetAllocatedBytes();
            // ReSharper disable once UnusedVariable
            var store       = new PocStore(hub);
            var diff        = Mem.GetAllocationDiff(start);
            var platform    = Environment.OSVersion.Platform;
            var isWindows   = platform == PlatformID.Win32NT; 
            var expected    = isWindows ? 880 : 880;  // Test Windows & Linux
            Console.WriteLine($"PocStore allocation. platform: {platform}, memory: {diff}");
            Mem.AreEqual(expected, diff);
        }
        
        /// <see cref="ObjectPool{T}.Get"/> returns a <see cref="Pooled{T}"/> <see cref="FlioxClient"/> or create a new one.
        /// When leaving the using scope { } it calls <see cref="FlioxClient.Reset"/>.
        /// => Same behavior as new <see cref="FlioxClient"/>.
        [Test]
        public void BenchmarkPooledClient() {
            var env             = new SharedEnv();
            var hub             = new NoopDatabaseHub("noop_db", env);
            var pocStorePool    = new ObjectPool<PocStore>(() => new PocStore(hub));
            FlioxClient client;
            using (var pooled = pocStorePool.Get()) {
                client = pooled.instance;
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int count = 1; // 10_000_000;
            for (int n = 0; n < count; n++) {
                using (var pooled = pocStorePool.Get()) // ~ 0.12 µs (Release)
                {
                    if (client != pooled.instance)
                        throw new InvalidOperationException ("Expect same reference");
                }
            }
            stopwatch.Stop();
            Console.WriteLine($"get pool client count: {count}, ms: {stopwatch.ElapsedMilliseconds}");
            
            var store = new PocStore(hub);
            var start = Mem.GetAllocatedBytes();
            store.Reset();
            var diff        = Mem.GetAllocationDiff(start);
            var platform    = Environment.OSVersion.Platform;
            Console.WriteLine($"PocStore Reset. platform: {platform}, memory: {diff}");
            Mem.AreEqual(72, diff);
        }
        
        [Test]
        public async Task TestMemorySync() {
            var env         = new SharedEnv();
            var hub         = new NoopDatabaseHub("noop_db", env);
            var store       = new PocStore(hub);
            await store.SyncTasks();                    // force one time allocations
            await store.SyncTasks();                    // force one time allocations
            // GC.Collect();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var start       = Mem.GetAllocatedBytes();
            int count = 1; // 1_000_000;
            for (int n = 0; n < count; n++) {
                await store.SyncTasks();                    // ~ 0.49 µs (Release)
            }
            var diff = Mem.GetAllocationDiff(start);
            stopwatch.Stop();
            Console.WriteLine($"SyncTasks() count: {count}, ms: {stopwatch.ElapsedMilliseconds}");
            var expected    = IsDebug() ? 568 : 408;  // Test Debug & Release
            Mem.AreEqual(expected, diff);
        }
        
        [Test]
        public async Task TestMemorySyncRead() {
            var env         = new SharedEnv();
            var database    = new MemoryDatabase("db");
            var hub         = new FlioxHub(database, env);
            var store       = new EntityIdStore(hub);
            var read = store.intEntities.Read();
            var ids = new int [100];
            for (int n = 0; n < 100; n++)
                ids[n] = n;
            read.FindRange(ids);
            await store.SyncTasks();                // force one time allocations
            
            long start = 0;
            for (int n = 0; n < 5; n++) {
                if (n > 0) start = Mem.GetAllocatedBytes();
                read = store.intEntities.Read();
                read.FindRange(ids);
                await store.SyncTasks();
            }
            var diff = Mem.GetAllocationDiff(start);
            var expected = IsDebug() ? new LongRange(14408, 14408) : new LongRange(11848, 11848);
            Mem.InRange(expected, diff);
        }
        
        [Test]
        public void TestSubscriptionProcessorMemory() {
            var sub         = new SubscriptionProcessor();
            var syncEvent   = new SyncEvent { db = new ShortString("dummy"), tasks = new List<SyncRequestTask>() };
            var db          = new MemoryDatabase("dummy");
            var hub         = new FlioxHub(db);
            var client      = new FlioxClient(hub);
            sub.ProcessEvent(client, syncEvent, 0);   // force initial allocations
            var start = Mem.GetAllocatedBytes();
            for (int n = 0; n < 10; n++) {
                sub.ProcessEvent(client, syncEvent, 0);
            }
            var diff = Mem.GetAllocationDiff(start);
            Mem.NoAlloc(diff);
        }
        
        class PooledClass : IDisposable {
            public void Dispose() { }
        }
        
        [Test]
        public void TestObjectPool() {
            var pool    = new ObjectPool<PooledClass>(() => new PooledClass());
            var pooled  = pool.Get();
            pool.Return(pooled.instance);

            var start   = Mem.GetAllocatedBytes();
            for (int n = 0; n < 10; n++) {
                pooled  = pool.Get();
                pool.Return(pooled.instance);
            }
            var diff = Mem.GetAllocationDiff(start);
            Mem.NoAlloc(diff);
        }
#endif
        
        public static bool IsDebug() {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
