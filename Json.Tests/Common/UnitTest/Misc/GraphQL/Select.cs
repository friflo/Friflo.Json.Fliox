using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
{
    
    public class TestSelect
    {

        [Test]
        public void ReadToDatabase() {
            var order = TestRelationPoC.CreateOrder("order-1");
            var db = new PocDatabase();
            
            using (var m = new JsonMapper()) {
                m.Pretty = true;
                var jsonOrder = m.Write(order);
                m.Database = db;
                var result = m.Read<Order>(jsonOrder);
                
                AreEqual(1, db.customers.Count);
                AreEqual(2, db.articles.Count);
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









