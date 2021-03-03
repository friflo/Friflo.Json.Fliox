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
            var order1 = TestRelationPoC.CreateOrder();
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
        
        [Test]
        public void TestSelectSameInstance() {
            var order1 = TestRelationPoC.CreateOrder();
            var orders = new List<Order> { order1 };

            var orderQuery =
                from order in orders
                select order;

            int n = 0;
            foreach (var order in orderQuery) {
                IsTrue(order == orders[n++]);
            }
        }
    }
}









