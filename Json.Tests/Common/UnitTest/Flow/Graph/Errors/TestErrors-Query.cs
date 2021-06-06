// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Sync;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Errors
{
    public partial class TestErrors
    {
        // ------ Run test individual - using a FileDatabase
        [Test] public async Task TestQueryTask      () { await Test(async (store, database) => await AssertQueryTask        (store, database)); }
       
        
        private static async Task AssertQueryTask(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            const string articleError = @"EntityErrors ~ count: 2
| ReadError: Article 'article-1', simulated read entity error
| ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16";
        
            const string article1ReadError      = "article-1";
            const string article2JsonError      = "article-2";
            const string readTaskError          = "read-task-error";
            
            var testArticles  = testDatabase.GetTestContainer("Article");
            var testCustomers = testDatabase.GetTestContainer("Customer");
            
            testArticles.readEntityErrors.Add(article2JsonError, (value) => value.SetJson(@"{""invalidJson"" XXX}"));
            testArticles.readEntityErrors.Add(article1ReadError, (value) => value.SetError(testArticles.ReadError(article1ReadError)));
            testCustomers.readTaskErrors. Add(readTaskError,     () => new CommandError{message = "simulated read task error"});

            var orders = store.orders;
            var articles = store.articles;

            var readOrders  = orders.Read()                                                         .TaskName("readOrders");
            var order1      = readOrders.Find("order-1")                                            .TaskName("order1");
            AreEqual("Find<Order> (id: 'order-1')", order1.Details);
            var allArticles             = articles.QueryAll()                                       .TaskName("allArticles");
            var producersTask           = allArticles.ReadRefs(a => a.producer);
            var hasOrderCamera          = orders.Query(o => o.items.Any(i => i.name == "Camera"))   .TaskName("hasOrderCamera");
            var ordersWithCustomer1     = orders.Query(o => o.customer.id == "customer-1")          .TaskName("ordersWithCustomer1");
            var read3                   = orders.Query(o => o.items.Count(i => i.amount < 1) > 0)   .TaskName("read3");
            var ordersAnyAmountLower2   = orders.Query(o => o.items.Any(i => i.amount < 2))         .TaskName("ordersAnyAmountLower2");
            var ordersAllAmountGreater0 = orders.Query(o => o.items.All(i => i.amount > 0))         .TaskName("ordersAllAmountGreater0");
            var orders2WithTaskError    = orders.Query(o => o.customer.id == readTaskError)         .TaskName("orders2WithTaskError");
            var order2CustomerError     = orders2WithTaskError.ReadRefs(o => o.customer);
            
            AreEqual("ReadTask<Order> (#ids: 1)",                                       readOrders              .Details);
            AreEqual("Find<Order> (id: 'order-1')",                                     order1                  .Details);
            AreEqual("QueryTask<Article> (filter: true)",                               allArticles             .Details);
            AreEqual("allArticles -> .producer",                                        producersTask           .Details);
            AreEqual("QueryTask<Order> (filter: .items.Any(i => i.name == 'Camera'))",  hasOrderCamera          .Details);
            AreEqual("QueryTask<Order> (filter: .customer == 'customer-1')",            ordersWithCustomer1     .Details);
            AreEqual("QueryTask<Order> (filter: .items.Count() > 0)",                   read3                   .Details);
            AreEqual("QueryTask<Order> (filter: .items.Any(i => i.amount < 2))",        ordersAnyAmountLower2   .Details);
            AreEqual("QueryTask<Order> (filter: .items.All(i => i.amount > 0))",        ordersAllAmountGreater0 .Details);
            AreEqual("QueryTask<Order> (filter: .customer == 'read-task-error')",       orders2WithTaskError    .Details);
            AreEqual("orders2WithTaskError -> .customer",                               order2CustomerError     .Details);

            var orderCustomer   = orders.RefPath(o => o.customer);
            var customer        = readOrders.ReadRefPath(orderCustomer);
            var customer2       = readOrders.ReadRefPath(orderCustomer);
            AreSame(customer, customer2);
            var customer3       = readOrders.ReadRef(o => o.customer);
            AreSame(customer, customer3);
            AreEqual("readOrders -> .customer", customer.ToString());

            var readOrders2     = orders.Read()                                                     .TaskName("readOrders2");
            var order2          = readOrders2.Find("order-2")                                       .TaskName("order2");
            var order2Customer  = readOrders2.ReadRefPath(orderCustomer);
            
            AreEqual("readOrders -> .customer",         customer        .Details);
            AreEqual("ReadTask<Order> (#ids: 1)",       readOrders2     .Details);
            AreEqual("Find<Order> (id: 'order-2')",     order2          .Details);
            AreEqual("readOrders2 -> .customer",        order2Customer  .Details);

            Exception e;
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Id; });
            AreEqual("ReadRefTask.Id requires Sync(). readOrders -> .customer", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Result; });
            AreEqual("ReadRefTask.Result requires Sync(). readOrders -> .customer", e.Message);

            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera.Results; });
            AreEqual("QueryTask.Result requires Sync(). hasOrderCamera", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera["arbitrary"]; });
            AreEqual("QueryTask[] requires Sync(). hasOrderCamera", e.Message);

            var producerEmployees = producersTask.ReadArrayRefs(p => p.employeeList);
            AreEqual("allArticles -> .producer -> .employees[*]", producerEmployees.Details);
            
            var sync = await store.TrySync(); // -------- Sync --------
            
            IsFalse(sync.Success);
            AreEqual("tasks: 14, failed: 5", sync.ToString());
            AreEqual(14, sync.tasks.Count);
            AreEqual(5,  sync.failed.Count);
            const string msg = @"Sync() failed with task errors. Count: 5
|- allArticles # EntityErrors ~ count: 2
|   ReadError: Article 'article-1', simulated read entity error
|   ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- allArticles -> .producer # EntityErrors ~ count: 2
|   ReadError: Article 'article-1', simulated read entity error
|   ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- orders2WithTaskError -> .customer # DatabaseError ~ read references failed: 'Order -> .customer' - simulated read task error
|- readOrders2 -> .customer # DatabaseError ~ read references failed: 'Order -> .customer' - simulated read task error
|- allArticles -> .producer -> .employees[*] # EntityErrors ~ count: 2
|   ReadError: Article 'article-1', simulated read entity error
|   ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16";
            AreEqual(msg, sync.Message);
            
            AreEqual(1,                 ordersWithCustomer1.Results.Count);
            NotNull(ordersWithCustomer1["order-1"]);
            
            AreEqual(1,                 ordersAnyAmountLower2.Results.Count);
            NotNull(ordersAnyAmountLower2["order-1"]);
            
            AreEqual(2,                 ordersAllAmountGreater0.Results.Count);
            NotNull(ordersAllAmountGreater0["order-1"]);


            IsFalse(allArticles.Success);
            AreEqual(2, allArticles.Error.entityErrors.Count);
            AreEqual(articleError, allArticles.Error.ToString());
            
            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = allArticles.Results; });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            te = Throws<TaskResultException>(() => { var _ = allArticles.Results["article-galaxy"]; });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            AreEqual(1,                 hasOrderCamera.Results.Count);
            AreEqual(3,                 hasOrderCamera["order-1"].items.Count);
    
            AreEqual("customer-1",      customer.Id);
            AreEqual("Smith Ltd.",      customer.Result.name);
                
            IsFalse(producersTask.Success);
            te = Throws<TaskResultException>(() => { var _ = producersTask.Results; });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
                
            IsFalse(producerEmployees.Success);
            te = Throws<TaskResultException>(() => { var _ = producerEmployees.Results; });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            IsTrue(readOrders2.Success);
            IsTrue(order2.Success);
            IsFalse(order2Customer.Success);
            AreEqual("read-task-error", readOrders2["order-2"].customer.id);
            AreEqual("read-task-error", order2.Result.customer.id);
            AreEqual("DatabaseError ~ read references failed: 'Order -> .customer' - simulated read task error", order2Customer.   Error.ToString());
            
            IsTrue(orders2WithTaskError.Success);
            IsFalse(order2CustomerError.Success);
            AreEqual("read-task-error", orders2WithTaskError.Results["order-2"].customer.id);
            AreEqual("DatabaseError ~ read references failed: 'Order -> .customer' - simulated read task error", order2CustomerError.  Error.ToString());
        }
    }
}