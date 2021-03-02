using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
{
    
    public class TestSelect : LeakTestsFixture
    {
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
                var json = m.Write(orders);
                AreEqual(json, jsonQuery);
                Console.WriteLine(json);
            }
        }
    }
}









