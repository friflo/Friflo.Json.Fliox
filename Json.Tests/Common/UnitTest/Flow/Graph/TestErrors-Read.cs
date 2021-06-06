// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Graph;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public partial class TestErrors
    {
        // ------ Test each topic individual - using a FileDatabase
        [Test] public async Task TestReadTask       () { await Test(async (store, database) => await AssertReadTask         (store, database)); }

                private static async Task AssertReadTask(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            const string articleError = @"EntityErrors ~ count: 2
| ReadError: Article 'article-1', simulated read entity error
| ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16";
            
            TestContainer testArticles = testDatabase.GetTestContainer("Article");
            const string article1ReadError      = "article-1";
            const string article2JsonError      = "article-2";
            const string articleInvalidJson     = "article-invalidJson";
            const string articleIdDontMatch     = "article-idDontMatch";
            
            testArticles.readEntityErrors.Add(article2JsonError,    (value) => value.SetJson(@"{""invalidJson"" XXX}"));
            testArticles.readEntityErrors.Add(article1ReadError,    (value) => value.SetError(testArticles.ReadError(article1ReadError)));
            testArticles.readEntityErrors.Add(articleInvalidJson,   (value) => value.SetJson(@"{""invalidJson"" YYY}"));
            testArticles.readEntityErrors.Add(articleIdDontMatch,   (value) => value.SetJson(@"{""id"": ""article-unexpected-id"""));
            
            var orders = store.orders;
            var readOrders = orders.Read();
            var order1Task = readOrders.Find("order-1");
            await store.Sync();
            
            // schedule ReadRefs on an already synced Read operation
            Exception e;
            var orderCustomer = orders.RefPath(o => o.customer);
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefPath(orderCustomer); });
            AreEqual("Task already synced. ReadTask<Order> (#ids: 1)", e.Message);
            var itemsArticle = orders.RefsPath(o => o.items.Select(a => a.article));
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefsPath(itemsArticle); });
            AreEqual("Task already synced. ReadTask<Order> (#ids: 1)", e.Message);
            
            // todo add Read() without ids 

            readOrders              = orders.Read()                                                 .TaskName("readOrders");
            var order1              = readOrders.Find("order-1")                                    .TaskName("order1");
            var articleRefsTask     = readOrders.ReadRefsPath(itemsArticle);
            var articleRefsTask2    = readOrders.ReadRefsPath(itemsArticle);
            AreSame(articleRefsTask, articleRefsTask2);
            
            var articleRefsTask3 = readOrders.ReadArrayRefs(o => o.items.Select(a => a.article));
            AreSame(articleRefsTask, articleRefsTask3);
            AreEqual("readOrders -> .items[*].article", articleRefsTask.ToString());

            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask["article-1"]; });
            AreEqual("ReadRefsTask[] requires Sync(). readOrders -> .items[*].article", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask.Results; });
            AreEqual("ReadRefsTask.Results requires Sync(). readOrders -> .items[*].article", e.Message);

            var articleProducerTask = articleRefsTask.ReadRefs(a => a.producer);
            AreEqual("readOrders -> .items[*].article -> .producer", articleProducerTask.ToString());

            var duplicateId     = "article-galaxy"; // support duplicate ids
            
            var readTask1       = store.articles.Read()                                             .TaskName("readTask1");
            var galaxy          = readTask1.Find(duplicateId)                                       .TaskName("galaxy");
            var article1        = readTask1.Find(article1ReadError)                                 .TaskName("article1");
            var article1And2    = readTask1.FindRange(new [] {article1ReadError, article2JsonError}).TaskName("article1And2");
            var articleSet      = readTask1.FindRange(new [] {duplicateId, duplicateId, "article-ipad"}).TaskName("articleSet");
            
            var readTask2       = store.articles.Read()                                             .TaskName("readTask2"); // separate Read without errors
            var galaxy2         = readTask2.Find(duplicateId)                                       .TaskName("galaxy2");
            
            var readTask3       = store.articles.Read()                                             .TaskName("readTask3");
            var invalidJson     = readTask3.Find(articleInvalidJson)                                .TaskName("invalidJson");
            var idDontMatch     = readTask3.Find(articleIdDontMatch)                                .TaskName("idDontMatch");

            // test throwing exception in case of task or entity errors
            try {
                await store.Sync(); // -------- Sync --------
                
                Fail("Sync() intended to fail - code cannot be reached");
            } catch (SyncResultException sre) {
                AreEqual(6, sre.failed.Count);
                const string expect = @"Sync() failed with task errors. Count: 6
|- readOrders -> .items[*].article # EntityErrors ~ count: 2
|   ReadError: Article 'article-1', simulated read entity error
|   ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- readOrders -> .items[*].article -> .producer # EntityErrors ~ count: 2
|   ReadError: Article 'article-1', simulated read entity error
|   ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- article1 # EntityErrors ~ count: 1
|   ReadError: Article 'article-1', simulated read entity error
|- article1And2 # EntityErrors ~ count: 2
|   ReadError: Article 'article-1', simulated read entity error
|   ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- invalidJson # EntityErrors ~ count: 1
|   ParseError: Article 'article-invalidJson', JsonParser/JSON error: Expected ':' after key. Found: Y path: 'invalidJson' at position: 16
|- idDontMatch # EntityErrors ~ count: 1
|   ParseError: Article 'article-idDontMatch', entity id does not match key. id: article-unexpected-id";
                AreEqual(expect, sre.Message);
            }
            
            IsTrue(readOrders.Success);
            AreEqual(3, readOrders.Results["order-1"].items.Count);
            // readOrders is successful
            // but resolving its Ref<>'s (.items[*].article and .items[*].article > .producer) failed:
            
            IsFalse(articleRefsTask.Success);
            AreEqual(articleError, articleRefsTask.Error.ToString());
            
            IsFalse(articleProducerTask.Success);
            AreEqual(articleError, articleProducerTask.Error.ToString());

            // readTask1 failed - A ReadTask<> fails, if any FindTask<> of it failed.
            TaskResultException te;
            
            IsFalse(readTask1.Success);
            te = Throws<TaskResultException>(() => { var _ = readTask1.Results; });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            IsTrue(articleSet.Success);
            AreEqual(2,             articleSet.Results.Count);
            AreEqual("Galaxy S10",  articleSet.Results[duplicateId].name);
            AreEqual("iPad Pro",    articleSet.Results["article-ipad"].name);

            IsTrue(galaxy.Success);
            AreEqual("Galaxy S10",  galaxy.Result.name);
            
            IsFalse(article1.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ReadError: Article 'article-1', simulated read entity error", article1.Error.ToString());

            te = Throws<TaskResultException>(() => { var _ = article1And2.Results; });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            // readTask2 succeed - All it FindTask<> were successful
            IsTrue(readTask2.Success);
            IsTrue(galaxy2.Success);
            AreEqual("Galaxy S10", galaxy2.Result.name); 
            
            IsFalse(invalidJson.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ParseError: Article 'article-invalidJson', JsonParser/JSON error: Expected ':' after key. Found: Y path: 'invalidJson' at position: 16", invalidJson.Error.ToString());
            
            IsFalse(idDontMatch.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ParseError: Article 'article-idDontMatch', entity id does not match key. id: article-unexpected-id", idDontMatch.Error.ToString());
            
        }
    }
}