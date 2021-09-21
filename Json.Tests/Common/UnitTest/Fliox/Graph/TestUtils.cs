// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Graph.Internal;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph
{
    public class TestUtils
    {
        [Test]
        public void TestQueryRef() {
            using (var __       = Pools.SharedPools) // for LeakTestsFixture
            using (var database = new MemoryDatabase())
            using (var store    = new PocStore(database, new TypeStore(), "TestQueryRef")) {
                var orders = store.orders;
                var customerId = orders.Query(o => o.customer.Key == "customer-1");
                AreEqual("QueryTask<Order> (filter: .customer == 'customer-1')", customerId.ToString());
                
                var e = Throws<NotSupportedException>(() => { var _ = orders.Query(o => o.customer.Entity == null); });
                AreEqual("Query using Ref<>.Entity intentionally not supported. Only Ref<>.id is valid: o.customer.Entity, expression: o => (o.customer.Entity == null)", e.Message);

                store.Sync().Wait();
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
                    var     json = new JsonUtf8("{\"myId\": \"123\"}");
                    var     result  = processor.ReplaceKey(json, "myId", false, "id", out JsonKey _, out _);
                    AreEqual("{\"id\":\"123\"}", result.AsString());
                } {
                    // --- return modified JSON
                    var     json =  new JsonUtf8("{\"myId\": \"111\"}");
                    var     result  = processor.ReplaceKey(json, "myId", true, "id", out JsonKey _, out _);
                    AreEqual("{\"id\":111}", result.AsString());
                } {
                    // --- return modified JSON
                    var     json = new JsonUtf8("{\"id\": 456}");
                    var     result  = processor.ReplaceKey(json, "id", false, "id", out JsonKey _, out _);
                    AreEqual("{\"id\":\"456\"}", result.AsString());
                } {
                    // --- return modified JSON - key ist not first member
                    var     json = new JsonUtf8("{\"x\":42,\"id2\":222}");
                    var     result  = processor.ReplaceKey(json, "id2", true, "id", out JsonKey _, out _);
                    AreEqual("{\"x\":42,\"id\":222}", result.AsString());
                } {
                    // --- return modified JSON - previous member contains unicode (☀), key is unicode (🌎)
                    var     json = new JsonUtf8("{\"☀\":1,\"🌎\": \"xyz\",\"♥\":2}");
                    var     result  = processor.ReplaceKey(json, "🌎", false, "🪐", out JsonKey _, out _);
                    AreEqual("{\"☀\":1,\"🪐\":\"xyz\",\"♥\":2}", result.AsString());
                } {
                    // --- return original JSON
                    var     json = new JsonUtf8("{\"id\": 789}");
                    var     result  = processor.ReplaceKey(json, "id", true, "id", out JsonKey _, out _);
                    IsTrue(json.IsEqualReference(result));
                } {
                    // --- return original JSON
                    var     json =  new JsonUtf8("{\"id\": \"abc\"}");
                    // null defaults to "id"
                    var result = processor.ReplaceKey(json, null, false, "id", out JsonKey _, out _);
                    IsTrue(json.IsEqualReference(result));
                }
            }
        }

#if !UNITY_2020_1_OR_NEWER
        [Test]
        public void TestDictionaryValueIterator() {
            var store = new PocStore(new MemoryDatabase(), new TypeStore(), "TestDictionaryValueIterator");
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
        public void TestMemoryEntityStore() {
            using (var typeStore = new TypeStore()) {
                var database    = new NoopDatabase();
                var _           = new PocStore(database, typeStore, null);
                var __          = new PocStore(database, typeStore, null);
                
                var start = GC.GetAllocatedBytesForCurrentThread();
                // ReSharper disable once UnusedVariable
                var store = new PocStore(database, typeStore, null); // ~ 6 µs
                var diff = GC.GetAllocatedBytesForCurrentThread() - start;
                
                Console.WriteLine($"PocStore memory: {diff}");
                var expected = Is.InRange(8536, 8880);
                That(diff, expected);
            }
        }

        [Test]
        public async Task TestMemorySync() {
            using (var typeStore = new TypeStore()) {
                var database    = new NoopDatabase();
                var store       = new PocStore(database, typeStore, null);
                await store.Sync(); // force one time allocations
                // GC.Collect();
                
                var start = GC.GetAllocatedBytesForCurrentThread();
                await store.Sync(); // ~ 1 µs
                var diff = GC.GetAllocatedBytesForCurrentThread() - start;
                var expected = IsDebug() ? 1352 : 1296; // Test Debug & Release
                AreEqual(expected, diff);   // Test Release also
            }
        }
        
        [Test]
        public async Task TestMemorySyncRead() {
            using (var typeStore = new TypeStore()) {
                var database    = new MemoryDatabase();
                var store       = new EntityIdStore(database, typeStore, null);
                var read = store.intEntities.Read();
                var ids = new int [100];
                for (int n = 0; n < 100; n++)
                    ids[n] = n;
                read.FindRange(ids);
                await store.Sync(); // force one time allocations
                
                var start = GC.GetAllocatedBytesForCurrentThread();
                for (int n = 0; n < 1; n++) {
                    read = store.intEntities.Read();
                    read.FindRange(ids);
                    await store.Sync();
                }
                var diff = GC.GetAllocatedBytesForCurrentThread() - start;
                var expected = IsDebug() ? Is.InRange(61288, 61640) : Is.InRange(58352, 58416); // Test Debug & Release
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

        private class NoopDatabase : EntityDatabase
        {
            public override EntityContainer CreateContainer(string name, EntityDatabase database) {
                return null;
            }
            
            public override Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
                var result = new SyncResponse {
                    tasks       = new List<SyncTaskResult>(),
                    resultMap   = new Dictionary<string, ContainerEntities>()
                };
                return Task.FromResult(result);
            }
        }
#endif
    }
}
