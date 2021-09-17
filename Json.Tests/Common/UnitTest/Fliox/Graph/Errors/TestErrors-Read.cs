// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Graph;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph.Errors
{
    public partial class TestErrors
    {
        // ------ Test each topic individual - using a FileDatabase
        [Test] public async Task TestReadTask       () { await Test(async (store, database) => await AssertReadTask         (store, database)); }

        private static async Task AssertReadTask(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            const string articleError = @"EntityErrors ~ count: 2
| ReadError: articles 'article-1', simulated read entity error
| ParseError: articles 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16";
            
            TestContainer testArticles = testDatabase.GetTestContainer(nameof(PocStore.articles));
            const string article1ReadError      = "article-1";
            const string article2JsonError      = "article-2";
            const string articleInvalidJson     = "article-invalidJson";
            const string articleIdDoesntMatch   = "article-idDoesntMatch";
            const string missingArticle         = "missing-article";
            
            testArticles.readEntityErrors.Add(article2JsonError,    (value) => value.SetJson(@"{""invalidJson"" XXX}"));
            testArticles.readEntityErrors.Add(article1ReadError,    (value) => value.SetError(testArticles.ReadError(article1ReadError)));
            testArticles.readEntityErrors.Add(articleInvalidJson,   (value) => value.SetJson(@"{""invalidJson"" YYY}"));
            testArticles.readEntityErrors.Add(articleIdDoesntMatch, (value) => value.SetJson(@"{""id"": ""article-unexpected-id"""));
            
            testArticles.missingResultErrors.Add(missingArticle);
            
            var orders      = store.orders;
            var articles    = store.articles;
            var readOrders  = orders.Read();
            var order1Task  = readOrders.Find("order-1");
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
            var orderArticles       = readOrders.ReadRefsPath(itemsArticle)                         .TaskName("orderArticles");
            var orderArticles2      = readOrders.ReadRefsPath(itemsArticle);
            AreSame(orderArticles, orderArticles2);
            
            var orderArticles3      = readOrders.ReadArrayRefs(o => o.items.Select(a => a.article));
            AreSame(orderArticles, orderArticles3);
            AreEqual("readOrders -> .items[*].article", orderArticles.Details);

            e = Throws<TaskNotSyncedException>(() => { var _ = orderArticles["article-1"]; });
            AreEqual("ReadRefsTask[] requires Sync(). orderArticles", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = orderArticles.Results; });
            AreEqual("ReadRefsTask.Results requires Sync(). orderArticles", e.Message);

            var articleProducer = orderArticles.ReadRefs(a => a.producer)                           .TaskName("articleProducer");
            AreEqual("orderArticles -> .producer", articleProducer.Details);

            var duplicateId     = "article-galaxy"; // support duplicate ids
            
            var readArticles    = articles.Read()                                                   .TaskName("readArticles");
            var galaxy          = readArticles.Find(duplicateId)                                    .TaskName("galaxy");
            var article1        = readArticles.Find(article1ReadError)                              .TaskName("article1");
            var article1And2    = readArticles.FindRange(new [] {article1ReadError, article2JsonError}).TaskName("article1And2");
            var articleSet      = readArticles.FindRange(new [] {duplicateId, duplicateId, "article-ipad"}).TaskName("articleSet");
            
            var readArticles2   = articles.Read()                                                   .TaskName("readArticles2"); // separate Read without errors
            var galaxy2         = readArticles2.Find(duplicateId)                                   .TaskName("galaxy2");
            
            var readArticles3   = articles.Read()                                                   .TaskName("readArticles3");
            var invalidJson     = readArticles3.Find(articleInvalidJson)                            .TaskName("invalidJson");
            var idDoesntMatch   = readArticles3.Find(articleIdDoesntMatch)                          .TaskName("idDoesntMatch");
            
            var readArticles4   = articles.Read()                                                   .TaskName("readArticles4");
            var missingEntity   = readArticles4.Find(missingArticle)                                .TaskName("missingEntity");

            // test throwing exception in case of task or entity errors
            try {
                AreEqual(11, store.Tasks.Count);
                await store.Sync(); // -------- Sync --------
                
                Fail("Sync() intended to fail - code cannot be reached");
            } catch (SyncResultException sre) {
                AreEqual(7, sre.failed.Count);
                const string expect = @"Sync() failed with task errors. Count: 7
|- orderArticles # EntityErrors ~ count: 2
|   ReadError: articles 'article-1', simulated read entity error
|   ParseError: articles 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- articleProducer # EntityErrors ~ count: 2
|   ReadError: articles 'article-1', simulated read entity error
|   ParseError: articles 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- article1 # EntityErrors ~ count: 1
|   ReadError: articles 'article-1', simulated read entity error
|- article1And2 # EntityErrors ~ count: 2
|   ReadError: articles 'article-1', simulated read entity error
|   ParseError: articles 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- invalidJson # EntityErrors ~ count: 1
|   ParseError: articles 'article-invalidJson', JsonParser/JSON error: Expected ':' after key. Found: Y path: 'invalidJson' at position: 16
|- idDoesntMatch # EntityErrors ~ count: 1
|   ParseError: articles 'article-idDoesntMatch', entity key mismatch. key: id, value: article-unexpected-id
|- missingEntity # EntityErrors ~ count: 1
|   ReadError: articles 'missing-article', requested entity missing in response results";
                AreEqual(expect, sre.Message);
            }
            
            IsTrue(readOrders.Success);
            AreEqual(3, readOrders.Results["order-1"].items.Count);
            // readOrders is successful
            // but resolving its Ref<>'s (.items[*].article and .items[*].article > .producer) failed:
            
            IsFalse(orderArticles.Success);
            AreEqual(articleError, orderArticles.Error.ToString());
            
            IsFalse(articleProducer.Success);
            AreEqual(articleError, articleProducer.Error.ToString());

            // readArticles failed - A ReadTask<> fails, if any FindTask<> of it failed.
            TaskResultException te;
            
            IsFalse(readArticles.Success);
            te = Throws<TaskResultException>(() => { var _ = readArticles.Results; });
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
| ReadError: articles 'article-1', simulated read entity error", article1.Error.ToString());

            te = Throws<TaskResultException>(() => { var _ = article1And2.Results; });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            // readArticles2 succeed - All it FindTask<> were successful
            IsTrue(readArticles2.Success);
            IsTrue(galaxy2.Success);
            AreEqual("Galaxy S10", galaxy2.Result.name); 
            
            IsFalse(invalidJson.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ParseError: articles 'article-invalidJson', JsonParser/JSON error: Expected ':' after key. Found: Y path: 'invalidJson' at position: 16", invalidJson.Error.ToString());
            
            IsFalse(idDoesntMatch.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ParseError: articles 'article-idDoesntMatch', entity key mismatch. key: id, value: article-unexpected-id", idDoesntMatch.Error.ToString());
            
            IsFalse(missingEntity.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ReadError: articles 'missing-article', requested entity missing in response results", missingEntity.Error.ToString());
            
        }
    }
}