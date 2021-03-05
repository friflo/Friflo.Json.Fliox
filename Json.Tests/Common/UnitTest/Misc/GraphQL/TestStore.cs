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
            using (var refDb = TestRelationPoC.CreateDatabase()) {
                var order = refDb.orders["order-1"];
                var cache = new EntityCache(refDb);

                // --- cache empty
                await WriteRead(order, cache);
                AssertCache(order, cache);

                // --- cache filled
                await WriteRead(order, cache);
                AssertCache(order, cache);
            }
        }
        
        private static async Task WriteRead(Order order, EntityCache cache) {
            using (var m = new JsonMapper(cache.typeStore)) {
                m.Pretty = true;
                m.EntityCache = cache;
                
                AssertWriteRead(m, order);
                await cache.Sync();
                AssertWriteRead(m, order.customer);
                AssertWriteRead(m, order.items[0]);
                AssertWriteRead(m, order.items[1]);
                AssertWriteRead(m, order.items[0].article);
                AssertWriteRead(m, order.items[1].article);
            }
        }

        private static void AssertCache(Order order, EntityCache cache) {
            var orders      = cache.GetContainer<Order>();
            var articles    = cache.GetContainer<Article>();
            var customers   = cache.GetContainer<Customer>();
            
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