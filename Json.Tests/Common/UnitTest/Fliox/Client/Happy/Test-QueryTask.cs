// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Cosmos;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable PossibleNullReferenceException
// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [Test] public async Task TestQuery      () { await TestCreate(async (store) => await AssertQuery            (store)); }
        
        private static bool lab = false;

        private static async Task AssertQuery(PocStore store) {
            var orders      = store.orders;
            var articles    = store.articles;
            var customers   = store.customers;
            var producers   = store.producers;
            var employees   = store.employees;
            var types       = store.types;

            var readOrders              = orders.Read()                                             .TaskName("readOrders");
            var order1                  = readOrders.Find("order-1"); //                            .TaskName("order1");
            AreEqual("Find<Order> (id: 'order-1')", order1.Details); 
            var allArticles             = articles.QueryAll()                                       .TaskName("allArticles");
            var filterAll               = new EntityFilter<Article>(a => true);
            var allArticles2            = articles.QueryByFilter(filterAll)                         .TaskName("allArticles2");
            var producersTask           = allArticles.ReadRelations(producers, a => a.producer);
            AreEqual("allArticles -> .producer", producersTask.ToString());
            var allArticlesLimit        = articles.QueryAll();
            allArticlesLimit.limit      = 2;

            var hasOrderCamera          = orders.Query(o => o.items.Any(i => i.name == "Camera"))   .TaskName("hasOrderCamera");
            var ordersWithCustomer1     = orders.Query(o => o.customer == "customer-1")             .TaskName("ordersWithCustomer1");
            var ordersItemsAmount       = orders.Query(o => o.items.Count(i => i.amount < 2) > 0)   .TaskName("read3");
            var ordersAnyAmountLowerFilter = new EntityFilter<Order>(o => o.items.Any(i => i.amount < 2));
            var ordersAnyAmountLower2   = orders.QueryByFilter(ordersAnyAmountLowerFilter)          .TaskName("ordersAnyAmountLower2");
            var ordersAllAmountGreater0 = orders.Query(o => o.items.All(i => i.amount > 0))         .TaskName("ordersAllAmountGreater0");
            
            var testEnumNullQuery       = types.Query(t => t.testEnumNull == TestEnum.e1);
            AreEqual("t => t.testEnumNull == 'e1'", testEnumNullQuery.filterLinq);
            var testTEnumQuery          = types.Query(t => t.testEnum == TestEnum.e2);
            AreEqual("t => t.testEnum == 'e2'", testTEnumQuery.filterLinq);
            
            var producersEmployees      = producers.Query(p => p.employeeList == null);
            AreEqual("p => p.employees == null", producersEmployees.filterLinq);

            // ensure API available
            AreEqual($"c['customer'] = \"customer-1\"",                                           ordersWithCustomer1.filter.CosmosFilter());
            AreEqual($"EXISTS(SELECT VALUE i FROM i IN c['items'] WHERE i['name'] = \"Camera\")", hasOrderCamera.filter.CosmosFilter()); 

            var orderCustomer           = orders.RelationPath(customers, o => o.customer);
            var customer                = readOrders.ReadRelation(customers, orderCustomer);
            var customer2               = readOrders.ReadRelation(customers, orderCustomer);
            AreSame(customer, customer2);
            var customer3               = readOrders.ReadRelation(customers, o => o.customer);
            AreSame(customer, customer3);
            AreEqual("readOrders -> .customer", customer.Label);

            Exception e;
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Result; });
            AreEqual("ReadRelation.Result requires SyncTasks(). readOrders -> .customer", e.Message);

            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera.Result; });
            AreEqual("QueryTask.Result requires SyncTasks(). hasOrderCamera", e.Message);

            var producerEmployees = producersTask.ReadRelations(employees, p => p.employeeList);
            AreEqual("allArticles -> .producer -> .employees[*]", producerEmployees.ToString());

            // lab - test ReadRef expressions
            if (lab) {
                // readOrders.ReadRefsOfType<Article>();
                // readOrders.ReadAllRefs();
            }

            await store.SyncTasks(); // ----------------
            AreEqual(1,                 ordersWithCustomer1.Result.Count);
            NotNull(ordersWithCustomer1.Result.Find(i => i.id == "order-1"));
            AreEqual(1,                 ordersWithCustomer1.RawResult.Length);
            AreEqual("order-1", ordersWithCustomer1.RawResult[0].key.AsString());
            
            AreEqual(1,                 ordersItemsAmount.Result.Count);
            NotNull(ordersItemsAmount.Result.Find(i => i.id == "order-1"));

            AreEqual(1,                 ordersAnyAmountLower2.Result.Count);
            NotNull(ordersAnyAmountLower2.Result.Find(i => i.id == "order-1"));
            
            AreEqual(2,                 ordersAllAmountGreater0.Result.Count);
            NotNull(ordersAllAmountGreater0.Result.Find(i => i.id == "order-1"));

            AreEqual(7,                 allArticles.Result.Count);
            AreEqual("Galaxy S10",      allArticles.Result.Find(i => i.id =="article-galaxy").name);
            AreEqual("iPad Pro",        allArticles.Result.Find(i => i.id =="article-ipad").name);
            
            AreEqual(2,                 allArticlesLimit.Result.Count);
            
            AreEqual(1,                 hasOrderCamera.Result.Count);
            AreEqual(3,                 hasOrderCamera.Result.Find(i => i.id == "order-1").items.Count);
    
            AreEqual("Smith Ltd.",      customer.Result.name);
            
            AreEqual(3,                 producersTask.Result.Count);
            AreEqual("Samsung",         producersTask.Result.Find(i => i.id == "producer-samsung").name);
            AreEqual("Canon",           producersTask.Result.Find(i => i.id == "producer-canon").name);
            AreEqual("Apple",           producersTask.Result.Find(i => i.id == "producer-apple").name);
                
            AreEqual(1,                 producerEmployees.Result.Count);
            AreEqual("Steve",           producerEmployees.Result.Find(i => i.id == "apple-0001").firstName);
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
            AreEqual(7, count);
            AreEqual(2, iterations);
            
            var closeCursors = articles.CloseCursors(null);
            await store.SyncTasks();
            
            var openCursors = closeCursors.Count; 
            AreEqual(0, openCursors);
        }
    }
}