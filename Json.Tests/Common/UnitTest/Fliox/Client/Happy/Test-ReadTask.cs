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

// ReSharper disable PossibleNullReferenceException
// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [Test] public async Task TestRead       () { await TestCreate(async (store) => await AssertRead             (store)); }
        
        /// Optimization: <see cref="RefPath{TEntity,TRefKey,TRef}"/> and <see cref="RefsPath{TEntity,TRefKey,TRef}"/> can be created static as creating
        /// a path from a <see cref="System.Linq.Expressions.Expression"/> is costly regarding heap allocations and CPU.
        private static readonly RefPath <Order, string, Customer> OrderCustomer = RefPath<Order, string, Customer>.MemberRef(o => o.customer);
        private static readonly RefsPath<Order, string, Article> ItemsArticle  =  RefsPath<Order, string, Article>.MemberRefs(o => o.items.Select(a => a.article));
        
        private static async Task AssertRead(PocStore store) {
            var orders = store.orders;
            var readOrders = orders.Read()                                      .TaskName("readOrders");
            var order1Task = readOrders.Find("order-1")                         .TaskName("order1Task");
            await store.SyncTasks();
            
            // schedule ReadRefs on an already synced Read operation
            Exception e;
            var orderCustomer = orders.RefPath(o => o.customer);
            AreEqual(OrderCustomer.path, orderCustomer.path);
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefPath(orderCustomer); });
            AreEqual("Task already executed. readOrders", e.Message);
            var itemsArticle = orders.RefsPath(o => o.items.Select(a => a.article));
            AreEqual(ItemsArticle.path, itemsArticle.path);
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefsPath(itemsArticle); });
            AreEqual("Task already executed. readOrders", e.Message);
            
            // todo add Read() without ids 

            readOrders              = orders.Read()                                             .TaskName("readOrders");
            var order1              = readOrders.Find("order-1")                                .TaskName("order1");
            var articleRefsTask     = readOrders.ReadRefsPath(itemsArticle);
            var articleRefsTask2    = readOrders.ReadRefsPath(itemsArticle);
            AreSame(articleRefsTask, articleRefsTask2);
            
            var articleRefsTask3 = readOrders.ReadArrayRefs(o => o.items.Select(a => a.article));
            AreSame(articleRefsTask, articleRefsTask3);
            AreEqual("readOrders -> .items[*].article", articleRefsTask.Details);

            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask.Result; });
            AreEqual("ReadRefsTask.Result requires SyncTasks(). readOrders -> .items[*].article", e.Message);

            var articleProducerTask = articleRefsTask.ReadRefs(a => a.producer);
            AreEqual("readOrders -> .items[*].article -> .producer", articleProducerTask.Details);

            var readTask        = store.articles.Read()                                     .TaskName("readTask");
            var duplicateId     = "article-galaxy"; // support duplicate ids
            var galaxy          = readTask.Find(duplicateId)                                .TaskName("galaxy");
            var article1And2    = readTask.FindRange(new [] {"article-1", "article-2"})     .TaskName("article1And2");
            var articleSetIds   = new [] {duplicateId, duplicateId, "article-ipad"};
            var articleSet      = readTask.FindRange(articleSetIds)                         .TaskName("articleSet");

            AreEqual(@"order1
readOrders -> .items[*].article
readOrders -> .items[*].article -> .producer
galaxy
article1And2
articleSet", string.Join("\n", store.Tasks));

            await store.SyncTasks(); // ----------------
        
            AreEqual(2,                 articleRefsTask.Result.Count);
            AreEqual("Changed name",    articleRefsTask.Result.Find(i => i.id == "article-1").name);
            AreEqual("Smartphone",      articleRefsTask.Result.Find(i => i.id == "article-2").name);
            
            AreEqual(1,                 articleProducerTask.Result.Count);
            AreEqual("Canon",           articleProducerTask.Result.Find(i => i.id == "producer-canon").name);

            AreEqual(2,                 articleSet.Result.Count);
            AreEqual("Galaxy S10",      articleSet["article-galaxy"].name);
            AreEqual("iPad Pro",        articleSet["article-ipad"].name);
            
            AreEqual("Galaxy S10",      galaxy.Result.name);
            
            AreEqual(2,                 article1And2.Result.Count);
            AreEqual("Smartphone",      article1And2["article-2"].name);
            
            AreEqual(4,                 readTask.Result.Count);
            AreEqual("Galaxy S10",      readTask["article-galaxy"].name);
        }
    }
}