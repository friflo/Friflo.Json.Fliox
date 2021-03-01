using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Mapper;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
{
    
    public class TestSelect
    {
       
        [Test]
        public void RunLinq() {
            var order1 = TestRelationPoC.CreateOrder("order-1");
            string lastName = order1.customer.lastName;
            var orders = new List<Order>();
            orders.Add(order1);

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
                // var json = m.Write(orderQuery);
                var json = m.Write(order1);
                Console.WriteLine(json);
            }
        }
    }
}









