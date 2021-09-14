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
                AreEqual(entities.entities[new JsonKey("int")].Json, result.entities[new JsonKey("int")].Json);
                AreEqual(entities.entities[new JsonKey("str")].Json, result.entities[new JsonKey("str")].Json);
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
                var expected = Is.InRange(7368, 8392);
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
                var expected = IsDebug() ? 1344 : 1288; // Test Debug & Release
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
                var expected = IsDebug() ? Is.InRange(61256, 61296) : Is.InRange(58320, 58360); // Test Debug & Release
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
                    tasks   = new List<TaskResult>(),
                    results = new Dictionary<string, ContainerEntities>()
                };
                return Task.FromResult(result);
            }
        }
#endif
    }
}
