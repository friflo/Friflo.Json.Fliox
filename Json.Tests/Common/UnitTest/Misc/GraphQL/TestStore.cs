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
        public void WriteRead() {
            var store = TestRelationPoC.CreateStore();
            var order = store.orders["order-1"];
            
            using (var typeStore  = new TypeStore())
            using (var m = new JsonMapper(typeStore)) {
                typeStore.typeResolver.AddGenericTypeMapper(RefMatcher.Instance);
                typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
                m.Pretty = true;
                m.EntityStore = store;
                
                AssertWriteRead(m, order);
                AssertWriteRead(m, order.customer);
                AssertWriteRead(m, order.items[0]);
                AssertWriteRead(m, order.items[1]);
                AssertWriteRead(m, order.items[0].article);
                AssertWriteRead(m, order.items[1].article);
                
                AreEqual(1, store.customers.Count);
                AreEqual(2, store.articles.Count);
                AreEqual(1, store.orders.Count);

                IsTrue(store.orders   ["order-1"]      == order);
                IsTrue(store.articles ["article-1"]    == order.items[0].article.Entity);
                IsTrue(store.articles ["article-2"]    == order.items[1].article.Entity);
                IsTrue(store.customers["customer-1"]   == order.customer.Entity);
            }
        }

        private static void AssertWriteRead<T>(JsonMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            // IsFalse(entity.Equals(result)); // references are not equal
        }
    }
}