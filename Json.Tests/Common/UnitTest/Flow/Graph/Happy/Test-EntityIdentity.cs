// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Graph;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public partial class TestStore
    {
        [Test] public async Task TestEntityIdentity () { await TestCreate(async (store) => await AssertEntityIdentity   (store)); }
        
        private static async Task AssertEntityIdentity(PocStore store) {
            var articles    = store.articles;
            var customers   = store.customers;
            var orders      = store.orders;
            
            var readOrders = store.orders.Read();
            var orderTask  = readOrders.Find("order-1");
            
            await store.Sync(); // -------- Sync --------
            
            var order = orderTask.Result;
            
            Exception e;
            e = Throws<TaskAlreadySyncedException>(() => { var _ = readOrders.Find("order-1"); });
            AreEqual("Task already synced. ReadTask<Order> (#ids: 1)", e.Message);

            var readOrders2     = orders.Read()                         .TaskName("readOrders2");
            var order1Task      = readOrders2.Find("order-1")           .TaskName("order1Task");

            var readArticles    = articles.Read()                       .TaskName("readArticles");
            var article1Task    = readArticles.Find("article-1")        .TaskName("article1Task");
            // var article1TaskRedundant   =  readArticles.ReadId("article-1");
            // AreSame(article1Task, article1TaskRedundant);
            
            var readCustomers   = customers.Read()                      .TaskName("readCustomers");
            var article2Task    = readArticles.Find("article-2")        .TaskName("article2Task");
            var customer1Task   = readCustomers.Find("customer-1")      .TaskName("customer1Task");
            var unknownTask     = readCustomers.Find("customer-missing").TaskName("unknownTask");

            await store.Sync(); // -------- Sync --------
            
            // AreEqual(1, store.customers.Count);
            // AreEqual(2, store.articles.Count);
            // AreEqual(1, store.orders.Count);

            AreSame(order1Task.     Result,   order);
            AreSame(customer1Task.  Result,   order.customer.Entity);
            AreSame(article1Task.   Result,   order.items[0].article.Entity);
            AreSame(article2Task.   Result,   order.items[1].article.Entity);
            IsNull(unknownTask.     Result);
        }
    }
}