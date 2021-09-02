// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Database;
using Friflo.Json.Fliox.Graph.Internal;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Sync;
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
            using (var store    = new PocStore(database, "TestQueryRef")) {
                var orders = store.orders;
                var customerId = orders.Query(o => o.customer.key == "customer-1");
                AreEqual("QueryTask<Order> (filter: .customer == 'customer-1')", customerId.ToString());
                
                var e = Throws<NotSupportedException>(() => { var _ = orders.Query(o => o.customer.Entity == null); });
                AreEqual("Query using Ref<>.Entity intentionally not supported. Only Ref<>.id is valid: o.customer.Entity, expression: o => (o.customer.Entity == null)", e.Message);

                store.Sync().Wait();
            }
        }

        [Test]
        public void TestEmptyDictionary() {
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

#if !UNITY_2020_1_OR_NEWER
        [Test]
        public void TestDictionaryValueIterator() {
            var store = new PocStore(new MemoryDatabase(), "TestDictionaryValueIterator");
            var readArticles = store.articles.Read();
                        readArticles.Find("missing-id");
            var task =  readArticles.ReadRef(a => a.producer);
            SubRefs subRefs;
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
                
                var start = GC.GetAllocatedBytesForCurrentThread();
                var store = new PocStore(database, typeStore, null); // ~ 4 µs
                var diff = GC.GetAllocatedBytesForCurrentThread() - start;
                
                Console.WriteLine($"PocStore memory: {diff}");
                IsTrue(diff < 8010);
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
#if DEBUG
                AreEqual(1344, diff);   // Test Release also
#else
                AreEqual(1288, diff);   // Test Debug also
#endif
            }
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
