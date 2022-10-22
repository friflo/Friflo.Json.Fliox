using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.TestLinq
{

    public class TestSelect : LeakTestsFixture
    {
        /// withdraw from allocation detection by <see cref="LeakTestsFixture"/> => init before tracking starts
        [NUnit.Framework.OneTimeSetUp]    public static void  Init()       { TestGlobals.Init(); }
        [NUnit.Framework.OneTimeTearDown] public static void  Dispose()    { TestGlobals.Dispose(); }
        
        [Test]
        public void RunLinq() {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new MemoryDatabase(TestGlobals.DB, new PocService()))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var env          = new SharedEnv())
            using (var store        = new PocStore(hub) { UserId = "store"})
            using (var m            = new ObjectMapper(env.TypeStore)) {
                TestRelationPoC.CreateStore(store).Wait();
                var readOrders = store.orders.Read(); 
                var order1 = readOrders.Find("order-1");
                store.SyncTasks().Wait();
                var orderResult = order1.Result;
                var orders = new List<Order> { orderResult };

                var orderQuery =
                    from order in orders
                    // where order.id == "order-1"
                    select new Order {
                        id          = order.id,
                        created     = new DateTime(2021, 7, 22, 6, 0, 0, DateTimeKind.Utc),
                        customer    = order.customer,
                        items       = order.items
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
            using (var database     = new MemoryDatabase(TestGlobals.DB, new PocService()))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var store        = new PocStore(hub) { UserId = "store"}) {
                TestRelationPoC.CreateStore(store).Wait();
                var readOrders = store.orders.Read(); 
                var order = readOrders.Find(id);
                store.SyncTasks().Wait();
                return order.Result;
            }
        }

        [Test]
        public void DebugLinqQuery() {
            using (var _ = SharedEnv.Default) // for LeakTestsFixture
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
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new MemoryDatabase(TestGlobals.DB, new PocService()))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var store        = new PocStore(hub) { UserId = "store" }) {
                TestRelationPoC.CreateStore(store).Wait();
                var readOrders = store.orders.Read(); 
                var order1 = readOrders.Find("order-1");
                store.SyncTasks().Wait();
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
        
        class TestChild
        {
            public int x;
            public int y;
        }
        
        class TestClass
        {
            public int a;
            public int b;
            public TestChild child;
        }
        
        public class PatchProjection<T>
        {
            public void Patch(Expression<Func<T, object>> member) {
                /* var path = new string[member.Length];
                for (int n = 0; n < member.Length; n++) {
                    path[n] = Operation.PathFromLambda(member[n], ClientStatic.RefQueryPath);
                }*/
            }
        }
        
        public static class TestProjection
        {
            [Test]
            public static void PatchTest() {
                
                var test = new PatchProjection<TestClass>();
                test.Patch(order => new { order.a, order.b, order.child }); 
            }
            
            public static void SuppressCompilerWarnings() {
                var child = new TestChild { x = 1, y = 2 };
                var test  = new TestClass { a = 1, b = 2, child = null };
            }
        }
    }
}
