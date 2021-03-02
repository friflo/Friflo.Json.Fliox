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
            var db = TestRelationPoC.CreateDB();
            
            using (var typeStore  = new TypeStore())
            using (var m = new JsonMapper(typeStore)) {
                typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
                m.Pretty = true;
                m.Database = db;
                var order = db.orders["order-1"];
                var result = AssertWriteRead(m, order);
                
                AssertUtils.Equivalent(order, result);
                AreEqual(1, db.customers.Count);
                AreEqual(2, db.articles.Count);
                AreEqual(1, db.orders.Count);
                
                // IsTrue(db.orders["order-1"]         == result);
                IsTrue(db.articles["article-1"]     == result.items[0].article);
                IsTrue(db.articles["article-2"]     == result.items[1].article);
                IsTrue(db.customers["customer-1"]   == result.customer);
                
                AssertWriteRead(m, order.customer);
                AssertWriteRead(m, order.items[0].article);
                AssertWriteRead(m, order.items[1].article);
            }
        }

        private static T AssertWriteRead<T>(JsonMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            IsFalse(entity.Equals(result)); // references are not equal
            return result;
        }
    }
}