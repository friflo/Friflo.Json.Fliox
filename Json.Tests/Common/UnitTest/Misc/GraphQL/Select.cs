using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
{
    
    public class TestSelect : LeakTestsFixture
    {

        [Test]
        public void ReadToDatabase() {
            var db = TestRelationPoC.CreateDB();
            
            using (var typeStore  = new TypeStore())
            using (var m = new JsonMapper(typeStore)) {
                typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
                m.Pretty = true;
                m.Database = db;
                var order = db.orders["order-1"];
                var jsonOrder = m.Write(order);
                var result =    m.Read<Order>(jsonOrder);
                
                AssertUtils.Equivalent(order, result);
                AreEqual(1, db.customers.Count);
                AreEqual(2, db.articles.Count);
                AreEqual(1, db.orders.Count);
                
                // IsTrue(db.orders["order-1"]         == result);
                IsTrue(db.articles["article-1"]     == result.items[0].article);
                IsTrue(db.articles["article-2"]     == result.items[1].article);
                IsTrue(db.customers["customer-1"]   == result.customer);
            }
        }

        [Test]
        public void RunLinq() {
            var db = TestRelationPoC.CreateDB();
            
            var order1 = db.orders["order-1"];
            var orders = new List<Order> { order1 };

            var orderQuery =
                from order in orders
                // where order.id == "order-1"
                select new Order {
                    id = order.id,
                    customer =  order.customer,
                    items = order.items
                };

            using (var m = new JsonMapper()) {
                m.Pretty = true;
                var jsonQuery = m.Write(orderQuery);
                var json = m.Write(order1);
                Console.WriteLine(json);
            }
        }
    }
}









