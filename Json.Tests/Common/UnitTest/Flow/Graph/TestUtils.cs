using System;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph.Internal;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestUtils
    {
        [Test]
        public void TestQueryRef() {
            using (var database = new MemoryDatabase())
            using (var store    = new PocStore(database)) {
                var orders = store.orders;
                var customerId = orders.Query(o => o.customer.id == "customer-1");
                AreEqual("QueryTask<Order> filter: .customer == 'customer-1'", customerId.ToString());
                
                var e = Throws<NotSupportedException>(() => { var _ = orders.Query(o => o.customer.Entity == null); });
                AreEqual("Query using Ref<>.Entity intentionally not supported. Only Ref<>.id is valid: o.customer.Entity, expression: o => (o.customer.Entity == null)", e.Message);

                store.SyncWait();
            }
        }

#if !UNITY_2020_1_OR_NEWER
        [Test]
        public void TestDictionaryValueIterator() {
            var store = new PocStore(new MemoryDatabase());
            var readArticles = store.articles.Read();
            var read= readArticles.ReadId("none");
            var task = readArticles.ReadRef(a => a.producer);
            SubRefs subRefs;
            subRefs.AddTask("someTask", task);

            // ensure iterator does not allocate something on heap by boxing
            var startBytes = GC.GetAllocatedBytesForCurrentThread();
            foreach (var subRef in subRefs) {
            }
            var endBytes = GC.GetAllocatedBytesForCurrentThread();
            AreEqual(startBytes, endBytes);
        }
#endif
    }
}
