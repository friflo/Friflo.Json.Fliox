// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable PossibleNullReferenceException
// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Errors
{
    public partial class TestErrors
    {
        // ------ Run test individual - using a FileDatabase
        [Test] public async Task TestQueryTask      () { await Test(async (store, database) => await AssertQueryTask        (store, database)); }
       
        
        private static async Task AssertQueryTask(PocStore store, TestDatabaseHub testHub) {
            testHub.ClearErrors();
            const string articleError = @"EntityErrors ~ count: 2
| ReadError: articles [article-1], simulated read entity error
| ParseError: articles [article-2], JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16";
        
            const string article1ReadError      = "article-1";
            const string article2JsonError      = "article-2";
            const string readTaskError          = "read-task-error";
            
            var testArticles  = testHub.GetTestContainer(nameof(PocStore.articles));
            var testCustomers = testHub.GetTestContainer(nameof(PocStore.customers));
            
            testArticles.readEntityErrors.Add(article2JsonError, new SimJson(@"{""invalidJson"" XXX}"));
            testArticles.readEntityErrors.Add(article1ReadError, new SimReadError());
            testCustomers.readTaskErrors. Add(readTaskError,     () => new TaskExecuteError("simulated read task error"));

            var orders      = store.orders;
            var articles    = store.articles;
            var producers   = store.producers;
            var customers   = store.customers;
            var employees   = store.employees;

            var readOrders  = orders.Read()                                                         .TaskName("readOrders");
            var order1      = readOrders.Find("order-1"); //                                         .TaskName("order1");
            AreEqual("Find<Order> (id: 'order-1')", order1.Details);
            var allArticles             = articles.QueryAll()                                       .TaskName("allArticles");
            var articleProducer         = allArticles.ReadRelations(producers, a => a.producer); //  .TaskName("articleProducer");
            var hasOrderCamera          = orders.Query(o => o.items.Any(i => i.name == "Camera"))   .TaskName("hasOrderCamera");
            var ordersWithCustomer1     = orders.Query(o => o.customer == "customer-1")             .TaskName("ordersWithCustomer1");
            var read3                   = orders.Query(o => o.items.Count(i => i.amount < 1) > 0)   .TaskName("read3");
            var ordersAnyAmountLower2   = orders.Query(o => o.items.Any(i => i.amount < 2))         .TaskName("ordersAnyAmountLower2");
            var ordersAllAmountGreater0 = orders.Query(o => o.items.All(i => i.amount > 0))         .TaskName("ordersAllAmountGreater0");
            var orders2WithTaskError    = orders.Query(o => o.customer == readTaskError)            .TaskName("orders2WithTaskError");
            var order2CustomerError     = orders2WithTaskError.ReadRelations(customers, o => o.customer); // .TaskName("order2CustomerError");
            
            AreEqual("ReadTask<Order> (ids: 1)",                                        readOrders              .Details);
            AreEqual("Find<Order> (id: 'order-1')",                                     order1                  .Details);
            AreEqual("QueryTask<Article> (filter: true)",                               allArticles             .Details);
            AreEqual("allArticles -> .producer",                                        articleProducer         .Label);
            AreEqual("QueryTask<Order> (filter: o => o.items.Any(i => i.name == 'Camera'))", hasOrderCamera          .Details);
            AreEqual("QueryTask<Order> (filter: o => o.customer == 'customer-1')",           ordersWithCustomer1     .Details);
            AreEqual("QueryTask<Order> (filter: o => o.items.Count(i => i.amount < 1) > 0)", read3                   .Details);
            AreEqual("QueryTask<Order> (filter: o => o.items.Any(i => i.amount < 2))",       ordersAnyAmountLower2   .Details);
            AreEqual("QueryTask<Order> (filter: o => o.items.All(i => i.amount > 0))",       ordersAllAmountGreater0 .Details);
            AreEqual("QueryTask<Order> (filter: o => o.customer == 'read-task-error')",      orders2WithTaskError    .Details);
            AreEqual("orders2WithTaskError -> .customer",                                    order2CustomerError     .Label);

            var orderCustomer   = orders.RelationPath(customers, o => o.customer);
            var customer        = readOrders.ReadRelation(customers, orderCustomer);
            var customer2       = readOrders.ReadRelation(customers, orderCustomer);
            AreSame(customer, customer2);
            var customer3       = readOrders.ReadRelation(customers, o => o.customer);
            AreSame(customer, customer3);
            AreEqual("readOrders -> .customer", customer.ToString());

            var readOrders2     = orders.Read()                                                     .TaskName("readOrders2");
            var order2          = readOrders2.Find("order-2"); //                                   .TaskName("order2");
            var order2Customer  = readOrders2.ReadRelation(customers, orderCustomer); //            .TaskName("order2Customer");
            
            AreEqual("readOrders -> .customer",         customer        .Label);
            AreEqual("ReadTask<Order> (ids: 1)",        readOrders2     .Details);
            AreEqual("Find<Order> (id: 'order-2')",     order2          .Details);
            AreEqual("readOrders2 -> .customer",        order2Customer  .Label);

            Exception e;
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Result; });
            AreEqual("ReadRelation.Result requires SyncTasks(). readOrders -> .customer", e.Message);

            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera.Result; });
            AreEqual("QueryTask.Result requires SyncTasks(). hasOrderCamera", e.Message);

            var producerEmployees = articleProducer.ReadRelations(employees, p => p.employeeList); //   .TaskName("producerEmployees");
            AreEqual("allArticles -> .producer -> .employees[*]", producerEmployees.Label);
            
            AreEqual(9,  store.Tasks.Count);
            var sync = await store.TrySyncTasks(); // ----------------
            
            IsFalse(sync.Success);
            AreEqual("tasks: 9, failed: 1", sync.ToString());
            AreEqual(9, sync.Tasks.Count);
            AreEqual(1, sync.Failed.Count);
            AreEqual(@"SyncTasks() failed with task errors. Count: 1
|- allArticles # EntityErrors ~ count: 2
|   ReadError: articles [article-1], simulated read entity error
|   ParseError: articles [article-2], JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16", sync.Message);
            
            AreEqual(@"EntityErrors ~ count: 2
| ReadError: articles [article-1], simulated read entity error
| ParseError: articles [article-2], JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16", articleProducer.Error.Message);
            
            AreEqual(@"DatabaseError ~ read references failed: 'orders -> .customer' - simulated read task error", order2CustomerError.Error.Message);
            AreEqual(@"DatabaseError ~ read references failed: 'orders -> .customer' - simulated read task error", order2Customer.Error.Message);
            AreEqual(@"EntityErrors ~ count: 2
| ReadError: articles [article-1], simulated read entity error
| ParseError: articles [article-2], JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16", producerEmployees.Error.Message);
            
            AreEqual(1,                 ordersWithCustomer1.Result.Count);
            NotNull(ordersWithCustomer1.Result.Find(i => i.id == "order-1"));
            
            AreEqual(1,                 ordersAnyAmountLower2.Result.Count);
            NotNull(ordersAnyAmountLower2.Result.Find(i => i.id == "order-1"));
            
            AreEqual(2,                 ordersAllAmountGreater0.Result.Count);
            NotNull(ordersAllAmountGreater0.Result.Find(i => i.id == "order-1"));


            IsFalse(allArticles.Success);
            AreEqual(2, allArticles.Error.entityErrors.Count);
            AreEqual(articleError, allArticles.Error.ToString());
            
            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = allArticles.Result; });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            te = Throws<TaskResultException>(() => { var _ = allArticles.Result.Find(i => i.id == "article-galaxy"); });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            AreEqual(1,                 hasOrderCamera.Result.Count);
            AreEqual(3,                 hasOrderCamera.Result.Find(i => i.id == "order-1").items.Count);
    
            AreEqual("Smith Ltd.",      customer.Result.name);
                
            IsFalse(articleProducer.Success);
            te = Throws<TaskResultException>(() => { var _ = articleProducer.Result; });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
                
            IsFalse(producerEmployees.Success);
            te = Throws<TaskResultException>(() => { var _ = producerEmployees.Result; });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            IsTrue(readOrders2.Success);
            IsTrue(order2.Success);
            IsFalse(order2Customer.Success);
            AreEqual("read-task-error", readOrders2.Result["order-2"].customer);
            AreEqual("read-task-error", order2.Result.customer);
            AreEqual("DatabaseError ~ read references failed: 'orders -> .customer' - simulated read task error", order2Customer.   Error.ToString());
            
            IsTrue(orders2WithTaskError.Success);
            IsFalse(order2CustomerError.Success);
            AreEqual("read-task-error", orders2WithTaskError.Result.Find(i => i.id == "order-2").customer);
            AreEqual("DatabaseError ~ read references failed: 'orders -> .customer' - simulated read task error", order2CustomerError.  Error.ToString());
        }
    }
}