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
                var dbOrder = db.orders.Read("order-1");
                var store = new PocStore(db);
                
                // --- cache empty
                var order = store.orders.Read("order-1");
                // await store.Sync();

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

        private static void AssertStore(Order order, PocStore store) {
            var orders      = store.orders;
            var articles    = store.articles;
            var customers   = store.customers;
            
            AreEqual(1, customers.Count);
            AreEqual(2, articles.Count);
            AreEqual(1, orders.Count);


            IsTrue(orders.   Read("order-1")      == order);
            IsTrue(articles. Read("article-1")    == order.items[0].article.Entity);
            IsTrue(articles. Read("article-2")    == order.items[1].article.Entity);
            IsTrue(customers.Read("customer-1")   == order.customer.Entity);
        }

        private static void AssertWriteRead<T>(JsonMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            // IsTrue(entity.Equals(result)); // references are equal
        }
    }
}