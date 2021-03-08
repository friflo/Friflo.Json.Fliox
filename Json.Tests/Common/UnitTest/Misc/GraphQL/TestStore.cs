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
            using (var store = await TestRelationPoC.CreateStore()) {
                
                // --- cache empty
                var order = store.orders.Read("order-1");
                await store.Sync();
                // await store.Sync();

                await WriteRead(order.Result, store);
                AssertStore(order.Result, store);
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
            var order1 =    store.orders.Read("order-1");
            var article1 =  store.articles.Read("article-1");
            var article2 =  store.articles.Read("article-1");
            var customer1 = store.customers.Read("customer-1");
            
            AreEqual(1, store.customers.Count);
            AreEqual(2, store.articles.Count);
            AreEqual(1, store.orders.Count);


            IsTrue(order1.Result      == order);
            IsTrue(article1.Result    == order.items[0].article.Entity);
            IsTrue(article1.Result    == order.items[1].article.Entity);
            IsTrue(customer1.Result   == order.customer.Entity);
        }

        private static void AssertWriteRead<T>(JsonMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            // IsTrue(entity.Equals(result)); // references are equal
        }
    }
}