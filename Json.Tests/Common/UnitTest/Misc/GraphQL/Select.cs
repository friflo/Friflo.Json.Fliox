using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.ER;
using Friflo.Json.Mapper.ER.Database;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
{
    public static class Query
    {
        public static T Opt<T>(
            int             limit = 0,
            Func<T, object> orderBy = null,
            Func<T, bool>   where = null,
            Func<T>         select = null
            ) where T : class
        {
            return select();
        }
    }


    public class TestSelect : LeakTestsFixture
    {
        private void ElaborateQueryApi() {
            var test1 = new Order {
                customer = {
                    Entity = {
                        id = default,
                        lastName = default
                    }
                },
                items = default
            };
            
            var test2 = new Order {
                customer = Query.Opt(
                    limit: 10,
                    orderBy: c => c.lastName,
                    where:   c => c.lastName == "dddd",
                    select:  () => new Customer {
                        lastName = default,
                        id = default }
                ),
                items = default
            };

            var test3 = Query.Opt(
                limit: 10,
                orderBy: o => o.customer.Entity.lastName,
                select: () => new Order {
                    customer = {
                        Entity = {
                            id = default,
                            lastName = default
                        }
                    },
                    items = default
                });
        }
        
        [Test]
        public void RunLinq() {
            using (var store = TestRelationPoC.CreateStore(new MemoryDatabase()).Result)
            using (var m = new JsonMapper(store.typeStore)) {
                var order1 = store.orders.Read("order-1");
                store.Sync();
                var orders = new List<Order> { order1.Result };

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
        
        private static Order GetOrder(string id) {
            using (var store = TestRelationPoC.CreateStore(new MemoryDatabase()).Result) {
                var order = store.orders.Read(id);
                store.Sync();
                return order.Result;
            }
        }

        [Test]
        public void DebugLinqQuery() {

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

        [Test]
        public async Task TestSelectSameInstance() {
            using (var store = await TestRelationPoC.CreateStore(new MemoryDatabase())) {
                var order1 = store.orders.Read("order-1");
                await store.Sync();
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









