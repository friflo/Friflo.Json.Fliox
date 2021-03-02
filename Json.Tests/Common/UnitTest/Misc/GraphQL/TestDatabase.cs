using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
{
    public class TestDatabase : LeakTestsFixture
    {
        [Test]
        public void WriteRead() {
            var order = TestRelationPoC.CreateOrder();
            var db = new PocDatabase();
            
            using (var typeStore  = new TypeStore())
            using (var m = new JsonMapper(typeStore)) {
                typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
                m.Pretty = true;
                m.Database = db;
                
                AssertWriteRead(m, order);
                AssertWriteRead(m, order.customer);
                AssertWriteRead(m, order.items[0]);
                AssertWriteRead(m, order.items[1]);
                AssertWriteRead(m, order.items[0].article);
                AssertWriteRead(m, order.items[1].article);
                
                AreEqual(1, db.customers.Count);
                AreEqual(2, db.articles.Count);
                AreEqual(1, db.orders.Count);
                
                IsTrue(db.orders["order-1"]         == order);
                IsTrue(db.articles["article-1"]     == order.items[0].article);
                IsTrue(db.articles["article-2"]     == order.items[1].article);
                IsTrue(db.customers["customer-1"]   == order.customer);
            }
        }

        private static void AssertWriteRead<T>(JsonMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            IsFalse(entity.Equals(result)); // references are not equal
        }
    }
}