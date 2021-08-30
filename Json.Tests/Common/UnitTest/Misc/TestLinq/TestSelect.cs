using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Database;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Graph;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.TestLinq
{

    public class TestSelect : LeakTestsFixture
    {
        [Test]
        public void RunLinq() {
            using (var _        = Pools.SharedPools) // for LeakTestsFixture
            using (var database = new MemoryDatabase())
            using (var store    = new PocStore(database, "store"))
            using (var m        = new ObjectMapper(store.TypeStore)) {
                TestRelationPoC.CreateStore(store).Wait();
                var readOrders = store.orders.Read(); 
                var order1 = readOrders.Find("order-1");
                store.Sync().Wait();
                var orderResult = order1.Result;
                var orders = new List<Order> { orderResult };

                var orderQuery =
                    from order in orders
                    // where order.id == "order-1"
                    select new Order {
                        id = order.id,
                        created = new DateTime(2021, 7, 22, 6, 0, 0, DateTimeKind.Utc),
                        customer =  order.customer,
                        items = order.items
                    };

                m.Pretty = true;
                var jsonQuery = m.Write(orderQuery);
                var json = m.Write(orders);
                AreEqual(json, jsonQuery);
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
        
        private static Order GetOrder(string id) {
            using (var database = new MemoryDatabase())
            using (var store = new PocStore(database, "store")) {
                TestRelationPoC.CreateStore(store).Wait();
                var readOrders = store.orders.Read(); 
                var order = readOrders.Find(id);
                store.Sync().Wait();
                return order.Result;
            }
        }

        [Test]
        public void DebugLinqQuery() {
            using (var _ = Pools.SharedPools) // for LeakTestsFixture
            {
                var order1 = GetOrder("order-1");

                var orders = new List<Order> {order1};

                IQueryable<Order> queryable = orders.AsQueryable(); // for illustration only: Create queryable explicit from orders

                var gqlOrders = new GqlEnumerable<Order>(queryable); // <=> new GqlEnumerable<Order>(orders)
                // var gqlOrders = new GqlEnumerable<Order>(orders);

                var orderQuery =
                    from order in gqlOrders
                    orderby order.customer
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
            using (var _        = Pools.SharedPools) // for LeakTestsFixture
            using (var database = new MemoryDatabase())
            using (var store    = new PocStore(database, "store")) {
                TestRelationPoC.CreateStore(store).Wait();
                var readOrders = store.orders.Read(); 
                var order1 = readOrders.Find("order-1");
                store.Sync().Wait();
                var orders = new List<Order> {order1.Result};

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
