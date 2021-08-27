// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
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

        class TestEntity : Entity { }

        [Test]
        public void TestSetEntityId() {
            var test = new TestEntity {
                id = "id-1" // OK
            };
            // changing id throws exception
            var e = Throws<ArgumentException>(() => { var _ = test.id = "id-2"; });
            AreEqual("Entity id must not be changed. Type: TestEntity, was: 'id-1', assigned: 'id-2'", e.Message);

            // setting id to the already used id is valid 
            test.id = "id-1";
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
        public Task TestSyncMemory() {
            var database = new NoopDatabase();
            var store = new PocStore(database, "TestSyncMemory");
            store.Sync(); // force one time allocations
            store.Sync();
            // GC.Collect();
            var start = GC.GetAllocatedBytesForCurrentThread();
            store.Sync();
            var diff = GC.GetAllocatedBytesForCurrentThread() - start;
#if DEBUG
            AreEqual(2608, diff);   // Test Release also
#else
            AreEqual(2552, diff);   // Test Debug also
#endif
            return Task.CompletedTask;
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
