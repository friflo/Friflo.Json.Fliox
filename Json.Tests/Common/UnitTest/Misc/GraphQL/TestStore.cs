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
        public void WriteReadFilledStore() {
            var referenceCache = TestRelationPoC.CreateCache();
            var order = referenceCache.orders["order-1"];
            WriteRead(order, referenceCache);
            AssertCache(order, referenceCache);
        }
        
        [Test]
        public void WriteReadEmptyStore() {
            var referenceStore = TestRelationPoC.CreateCache();
            var order = referenceStore.orders["order-1"];
            var cache = new PocCache();
            WriteRead(order, cache);
            // AssertCache(order,cache);
        }

        private static void WriteRead(Order order, PocCache cache) {
            using (var typeStore  = new TypeStore())
            using (var m = new JsonMapper(typeStore)) {
                typeStore.typeResolver.AddGenericTypeMapper(RefMatcher.Instance);
                typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
                m.Pretty = true;
                m.EntityCache = cache;
                
                AssertWriteRead(m, order);
                AssertWriteRead(m, order.customer);
                AssertWriteRead(m, order.items[0]);
                AssertWriteRead(m, order.items[1]);
                AssertWriteRead(m, order.items[0].article);
                AssertWriteRead(m, order.items[1].article);
            }
        }

        private static void AssertCache(Order order, PocCache cache) {
            AreEqual(1, cache.customers.Count);
            AreEqual(2, cache.articles.Count);
            AreEqual(1, cache.orders.Count);

            IsTrue(cache.orders   ["order-1"]      == order);
            IsTrue(cache.articles ["article-1"]    == order.items[0].article.Entity);
            IsTrue(cache.articles ["article-2"]    == order.items[1].article.Entity);
            IsTrue(cache.customers["customer-1"]   == order.customer.Entity);
        }

        private static void AssertWriteRead<T>(JsonMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            // IsFalse(entity.Equals(result)); // references are not equal
        }
    }
}