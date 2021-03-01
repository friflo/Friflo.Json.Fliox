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
            var order = TestRelationPoC.CreateOrder("order-1");
            var db = new PocDatabase();
            
            using (var typeStore  = new TypeStore())
            using (var m = new JsonMapper(typeStore)) {
                typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
                m.Pretty = true;
                var jsonOrder = m.Write(order);
                m.Database = db;
                var result = m.Read<Order>(jsonOrder);
                
                AssertUtils.Equivalent(order, result);
                AreEqual(1, db.customers.Count);
                AreEqual(2, db.articles.Count);
                AreEqual(1, db.orders.Count);
            }
        }

        [Test]
        public void RunLinq() {
            var order1 = TestRelationPoC.CreateOrder("order-1");
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









