using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Graph.Database;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Flow.Graph;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.UnitTest.Misc.TestLinq.Graph;



namespace Friflo.Json.Tests.Common.UnitTest.Misc.TestLinq
{

    public class TestSelect : LeakTestsFixture
    {
        [Test]
        public void ElaborateQueryApi() {
           
            var query1 = TestQuery(
                limit:      10,
                orderBy:    c => c.lastName,
                order:      Misc.TestLinq.Order.Asc,
                @where:      c => c.lastName == "dddd",
                @select:  () => new Customer {
                    lastName    = default,
                    id          = default }
            );

            var query2 = TestQuery(
                limit:      10,
                orderBy:    o => o.customer.Entity.lastName,
                order:      Misc.TestLinq.Order.Desc,
                @select: () => new Flow.Graph.Order {
                    customer = {
                        Entity = {
                            id          = default,
                            lastName    = default
                        }
                    },
                    items = default
                });
        }
        
        [Test]
        public void RunLinq() {
            using (var database = new MemoryDatabase())
            using (var store = TestRelationPoC.CreateStore(database).Result)
            using (var m = new ObjectMapper(store.TypeStore)) {
                var order1 = store.orders.Read("order-1");
                store.SyncWait();
                var orderResult = order1.Result;
                var orders = new List<Flow.Graph.Order> { orderResult };

                var orderQuery =
                    from order in orders
                    // where order.id == "order-1"
                    select new Flow.Graph.Order {
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

        private static bool WhereOrderEqual(Flow.Graph.Order order, string test) {
            return order.id == test;
        }

        private static Flow.Graph.Order SelectOrder(Flow.Graph.Order order) {
            return order;
        }
        
        private static Flow.Graph.Order GetOrder(string id) {
            using (var database = new MemoryDatabase())
            using (var store = TestRelationPoC.CreateStore(database).Result) {
                var order = store.orders.Read(id);
                store.SyncWait();
                return order.Result;
            }
        }

        [Test]
        public void DebugLinqQuery() {

            var order1 = GetOrder("order-1");

            var orders = new List<Flow.Graph.Order> {order1};

            IQueryable<Flow.Graph.Order> queryable = orders.AsQueryable(); // for illustration only: Create queryable explicit from orders

            var gqlOrders = new GqlEnumerable<Flow.Graph.Order>(queryable); // <=> new GqlEnumerable<Order>(orders)
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
            using (var database = new MemoryDatabase())
            using (var store = await TestRelationPoC.CreateStore(database)) {
                var order1 = store.orders.Read("order-1");
                await store.Sync();
                var orders = new List<Flow.Graph.Order> {order1.Result};

                var orderQuery =
                    from order in orders
                    select order;

                int n = 0;
                foreach (var order in orderQuery) {
                    IsTrue(order == orders[n++]);
                }
            }
        }
        
        [Test]
        public void TestUpdateField() {
            var order = new Flow.Graph.Order();
            order.customer = new Ref<Customer>();

            Update (order, o => o.customer.Entity.lastName);
            Update (order, o => o.items[1].amount);
            
            Update2 (order, o => new {
                o.customer.Entity.lastName,
                o.customer.id,
                item = o.items.Sel(i => new {
                    i.amount,
                    i.article,
                    i.article.Entity
                }),
            });
            
            Update3 (order, () => new Flow.Graph.Order{ items = null});


            int index = 3;
            Update2 (order, o => o.items[index].amount);
        }
    }
}









