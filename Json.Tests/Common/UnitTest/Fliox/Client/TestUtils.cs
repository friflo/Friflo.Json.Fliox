// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class TestUtils
    {
        [Test]
        public void TestQueryRef() {
            using (var __       = UtilsInternal.SharedPools) // for LeakTestsFixture
            using (var database = new MemoryDatabase())
            using (var hub      = new FlioxHub(database))
            using (var store    = new PocStore(hub, new TypeStore()) { UserId = "TestQueryRef"}) {
                var orders = store.orders;
                var customerId = orders.Query(o => o.customer.Key == "customer-1");
                AreEqual("QueryTask<Order> (filter: .customer == 'customer-1')", customerId.ToString());
                
                var e = Throws<NotSupportedException>(() => { var _ = orders.Query(o => o.customer.Entity == null); });
                AreEqual("Query using Ref<>.Entity intentionally not supported. Only Ref<>.id is valid: o.customer.Entity, expression: o => (o.customer.Entity == null)", e.Message);

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
        public void TestJsonEntities() {
            using (var typeStore = new TypeStore())
            using (var mapper = new ObjectMapper(typeStore)) {
                JsonEntities entities = new JsonEntities(2);
                entities.entities.Add(new JsonKey("int"), new EntityValue("1"));
                entities.entities.Add(new JsonKey("str"), new EntityValue("\"hello\""));
                var json = mapper.Write(entities);
                AreEqual("{\"int\":1,\"str\":\"hello\"}", json);
                
                var result = mapper.Read<JsonEntities>(json);
                AreEqual(entities.entities[new JsonKey("int")].Json.AsString(), result.entities[new JsonKey("int")].Json.AsString());
                AreEqual(entities.entities[new JsonKey("str")].Json.AsString(), result.entities[new JsonKey("str")].Json.AsString());
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
                }
            }
        }

#if !UNITY_2020_1_OR_NEWER
        [Test]
        public void TestDictionaryValueIterator() {
            var hub     = new FlioxHub(new MemoryDatabase());
            var store   = new PocStore(hub, new TypeStore()) { UserId = "TestDictionaryValueIterator"};
            var readArticles = store.articles.Read();
                        readArticles.Find("missing-id");
            var task =  readArticles.ReadRef(a => a.producer);
            SubRefs subRefs = new SubRefs();
            subRefs.AddTask("someTask", task);

            // ensure iterator does not allocate something on heap by boxing
            var startBytes = GC.GetAllocatedBytesForCurrentThread();
            foreach (var _ in subRefs) {
            }
            var endBytes = GC.GetAllocatedBytesForCurrentThread();
            AreEqual(startBytes, endBytes);
        }
        
        [Test]
        public void TestMemoryFlioxClient() {
            using (var typeStore = new TypeStore()) {
                var hub         = new NoopDatabaseHub();
                var _           = new PocStore(hub, typeStore);
                var __          = new PocStore(hub, typeStore);
                
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                int count = 1;
                for (int n = 0; n < count; n++) {
                    new PocStore(hub, typeStore); // ~ 1.9 µs (Release)
                }
                stopwatch.Stop();
                Console.WriteLine($"client instantiation count: {count}, ms: {stopwatch.ElapsedMilliseconds}");
                
                var start = GC.GetAllocatedBytesForCurrentThread();
                // ReSharper disable once UnusedVariable
                var store = new PocStore(hub, typeStore);
                var diff = GC.GetAllocatedBytesForCurrentThread() - start;
                var platform    = Environment.OSVersion.Platform;
                var isWindows   = platform == PlatformID.Win32NT; 
                var expected    = isWindows ? 3112 : 2896;
                Console.WriteLine($"PocStore allocation. platform: {platform}, memory: {diff}");
                AreEqual(expected, diff);
            }
        }

        [Test]
        public async Task TestMemorySync() {
            using (var typeStore = new TypeStore()) {
                var hub         = new NoopDatabaseHub();
                var store       = new PocStore(hub, typeStore);
                await store.SyncTasks(); // force one time allocations
                // GC.Collect();
                
                var start = GC.GetAllocatedBytesForCurrentThread();
                await store.SyncTasks(); // ~ 1 µs
                var diff = GC.GetAllocatedBytesForCurrentThread() - start;
                var expected = IsDebug() ? 1608 : 1504; // Test Debug & Release
                AreEqual(expected, diff);   // Test Release also
            }
        }
        
        [Test]
        public async Task TestMemorySyncRead() {
            using (var typeStore = new TypeStore()) {
                var database    = new MemoryDatabase();
                var hub         = new FlioxHub(database);
                var store       = new EntityIdStore(hub, typeStore);
                var read = store.intEntities.Read();
                var ids = new int [100];
                for (int n = 0; n < 100; n++)
                    ids[n] = n;
                read.FindRange(ids);
                await store.SyncTasks(); // force one time allocations
                
                var start = GC.GetAllocatedBytesForCurrentThread();
                for (int n = 0; n < 1; n++) {
                    read = store.intEntities.Read();
                    read.FindRange(ids);
                    await store.SyncTasks();
                }
                var diff = GC.GetAllocatedBytesForCurrentThread() - start;
                var expected = IsDebug() ? Is.InRange(47200, 47448) : Is.InRange(44280, 44480); // Test Debug & Release
                That(diff, expected);
            }
        }
        
        private static bool IsDebug() {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

#endif
    }
}
