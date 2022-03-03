// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [Test] public async Task TestQuery      () { await TestCreate(async (store) => await AssertQuery            (store)); }
        
        private static bool lab = false;

        private static async Task AssertQuery(PocStore store) {
            var orders = store.orders;
            var articles = store.articles;

            var readOrders              = orders.Read()                                             .TaskName("readOrders");
            var order1                  = readOrders.Find("order-1")                                .TaskName("order1");
            AreEqual("Find<Order> (id: 'order-1')", order1.Details); 
            var allArticles             = articles.QueryAll()                                       .TaskName("allArticles");
            var filterAll               = new EntityFilter<Article>(a => true);
            var allArticles2            = articles.QueryByFilter(filterAll)                         .TaskName("allArticles2");
            var producersTask           = allArticles.ReadRefs(a => a.producer);
            var allArticlesLimit        = articles.QueryAll();
            allArticlesLimit.limit      = 2;

            var hasOrderCamera          = orders.Query(o => o.items.Any(i => i.name == "Camera"))   .TaskName("hasOrderCamera");
            var ordersWithCustomer1     = orders.Query(o => o.customer.Key == "customer-1")         .TaskName("ordersWithCustomer1");
            var ordersItemsAmount       = orders.Query(o => o.items.Count(i => i.amount < 2) > 0)   .TaskName("read3");
            var ordersAnyAmountLowerFilter = new EntityFilter<Order>(o => o.items.Any(i => i.amount < 2));
            var ordersAnyAmountLower2   = orders.QueryByFilter(ordersAnyAmountLowerFilter)          .TaskName("ordersAnyAmountLower2");
            var ordersAllAmountGreater0 = orders.Query(o => o.items.All(i => i.amount > 0))         .TaskName("ordersAllAmountGreater0");
            
            // ensure API available
            AreEqual($"c.customer = 'customer-1'",                                        ordersWithCustomer1.DebugQuery.Cosmos);
            AreEqual($"EXISTS(SELECT VALUE i FROM i IN c.items WHERE i.name = 'Camera')", hasOrderCamera.DebugQuery.Cosmos); 

            var orderCustomer           = orders.RefPath(o => o.customer);
            var customer                = readOrders.ReadRefPath(orderCustomer);
            var customer2               = readOrders.ReadRefPath(orderCustomer);
            AreSame(customer, customer2);
            var customer3               = readOrders.ReadRef(o => o.customer);
            AreSame(customer, customer3);
            AreEqual("readOrders -> .customer", customer.Details);

            Exception e;
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Key; });
            AreEqual("ReadRefTask.Key requires SyncTasks(). readOrders -> .customer", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Result; });
            AreEqual("ReadRefTask.Result requires SyncTasks(). readOrders -> .customer", e.Message);

            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera.Result; });
            AreEqual("QueryTask.Result requires SyncTasks(). hasOrderCamera", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera["arbitrary"]; });
            AreEqual("QueryTask[] requires SyncTasks(). hasOrderCamera", e.Message);

            var producerEmployees = producersTask.ReadArrayRefs(p => p.employeeList);
            AreEqual("allArticles -> .producer -> .employees[*]", producerEmployees.ToString());

            // lab - test ReadRef expressions
            if (lab) {
                // readOrders.ReadRefsOfType<Article>();
                // readOrders.ReadAllRefs();
            }

            await store.SyncTasks(); // ----------------
            AreEqual(1,                 ordersWithCustomer1.Result.Count);
            NotNull(ordersWithCustomer1["order-1"]);
            
            AreEqual(1,                 ordersItemsAmount.Result.Count);
            NotNull(ordersItemsAmount["order-1"]);

            AreEqual(1,                 ordersAnyAmountLower2.Result.Count);
            NotNull(ordersAnyAmountLower2["order-1"]);
            
            AreEqual(2,                 ordersAllAmountGreater0.Result.Count);
            NotNull(ordersAllAmountGreater0["order-1"]);

            AreEqual(6,                 allArticles.Result.Count);
            AreEqual("Galaxy S10",      allArticles.Result["article-galaxy"].name);
            AreEqual("iPad Pro",        allArticles.Result["article-ipad"].name);
            
            AreEqual(2,                 allArticlesLimit.Result.Count);
            
            AreEqual(1,                 hasOrderCamera.Result.Count);
            AreEqual(3,                 hasOrderCamera["order-1"].items.Count);
    
            AreEqual("customer-1",      customer.Key);
            AreEqual("Smith Ltd.",      customer.Result.name);
                
            AreEqual(3,                 producersTask.Result.Count);
            AreEqual("Samsung",         producersTask["producer-samsung"].name);
            AreEqual("Canon",           producersTask["producer-canon"].name);
            AreEqual("Apple",           producersTask["producer-apple"].name);
                
            AreEqual(1,                 producerEmployees.Result.Count);
            AreEqual("Steve",           producerEmployees["apple-0001"].firstName);
        }
        
        [Test] public async Task TestQueryCursor      () { await TestCreate(async (store) => await AssertQueryCursor            (store)); }

        private static async Task AssertQueryCursor(PocStore store) {
            var articles    = store.articles;
            var queryAll    = articles.QueryAll();
            var count       = 0;
            var iterations  = 0;
            while (true) {
                queryAll.maxCount   = 4;
                iterations++;
            
                await store.SyncTasks();
                
                count          += queryAll.Result.Count;
                var cursor      = queryAll.ResultCursor;
                if (cursor == null)
                    break;
                queryAll        = articles.QueryAll();
                queryAll.cursor = cursor;
            }
            AreEqual(6, count);
            AreEqual(2, iterations);
            
            var closeCursors = articles.CloseCursors(null);
            await store.SyncTasks();
            
            var openCursors = closeCursors.Count; 
            AreEqual(0, openCursors);
        }
    }
}