using System.Threading.Tasks;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.ER;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
{
    public class TestStore : LeakTestsFixture
    {
        [Test]
        public async Task WriteRead() {
            using (var db = TestRelationPoC.CreateDatabase()) {
                var order = db.orders["order-1"];
                var store = new EntityStore(db);

                // --- cache empty
                await WriteRead(order, store);
                AssertStore(order, store);

                // --- cache filled
                await WriteRead(order, store);
                AssertStore(order, store);
            }
        }
        
        private static async Task WriteRead(Order order, EntityStore store) {
            using (var m = new JsonMapper(store.typeStore)) {
                m.Pretty = true;
                m.EntityStore = store;
                
                AssertWriteRead(m, order);
                await store.Sync();
                AssertWriteRead(m, order.customer);
                AssertWriteRead(m, order.items[0]);
                AssertWriteRead(m, order.items[1]);
                AssertWriteRead(m, order.items[0].article);
                AssertWriteRead(m, order.items[1].article);
            }
        }

        private static void AssertStore(Order order, EntityStore store) {
            var orders      = store.GetContainer<Order>();
            var articles    = store.GetContainer<Article>();
            var customers   = store.GetContainer<Customer>();
            
            AreEqual(1, customers.Count);
            AreEqual(2, articles.Count);
            AreEqual(1, orders.Count);

            IsTrue(orders   ["order-1"]      == order);
            IsTrue(articles ["article-1"]    == order.items[0].article.Entity);
            IsTrue(articles ["article-2"]    == order.items[1].article.Entity);
            IsTrue(customers["customer-1"]   == order.customer.Entity);
        }

        private static void AssertWriteRead<T>(JsonMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            // IsFalse(entity.Equals(result)); // references are not equal
        }
    }
}