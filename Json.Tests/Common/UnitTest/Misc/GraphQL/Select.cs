using System;
using System.Collections;
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
            using (var db = TestRelationPoC.CreateDatabase())
            using (var m = new JsonMapper(db.typeStore)) {
                var order1 = db.orders.Get("order-1");
                var orders = new List<Order> { order1 };

                var orderQuery =
                    from order in orders
                    // where order.id == "order-1"
                    select new Order {
                        id = order.id,
                        customer =  order.customer,
                        items = order.items
                    };

            
                m.Pretty = true;
                var jsonQuery = m.Write(orderQuery);
                var json = m.Write(orders);
                AreEqual(json, jsonQuery);
                Console.WriteLine(json);
            }
        }

        class GqlEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> enumerable;
            
            public GqlEnumerable(IEnumerable<T> enumerable) {
                this.enumerable = enumerable;
            }
            
            public IEnumerator<T> GetEnumerator() {
                return enumerable.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }

        private static bool WhereOrderEqual(Order order, string test) {
            return order.id == test;
        }

        private static Order SelectOrder(Order order) {
            return order;
        }

        [Test]
        public void DebugLinqQuery() {
            using (var db = TestRelationPoC.CreateDatabase()) {
                var order1 = db.orders.Get("order-1");
                var orders = new List<Order> {order1};

                IQueryable<Order> queryable = orders.AsQueryable(); // for illustration only: Create queryable explicit from orders

                var gqlOrders = new GqlEnumerable<Order>(queryable); // <=> new GqlEnumerable<Order>(orders)
                // var gqlOrders = new GqlEnumerable<Order>(orders);

                var orderQuery =
                    from order in gqlOrders
                    where WhereOrderEqual(order, "order-1") // where  order.id == "order-1"
                    select SelectOrder(order); // select order;

                int n = 0;
                foreach (var order in orderQuery) {
                    n++;
                    IsTrue(order1 == order);
                }
                AreEqual(1, n);
            }
        }

        [Test]
        public void TestSelectSameInstance() {
            using (var db = TestRelationPoC.CreateDatabase()) {
                var order1 = db.orders.Get("order-1");
                var orders = new List<Order> {order1};

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
}









