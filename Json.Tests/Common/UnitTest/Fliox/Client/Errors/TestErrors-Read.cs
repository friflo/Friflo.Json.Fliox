// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Errors
{
    public partial class TestErrors
    {
        // ------ Test each topic individual - using a FileDatabase
        [Test] public async Task TestReadTask       () { await Test(async (store, database) => await AssertReadTask         (store, database)); }

        private static async Task AssertReadTask(PocStore store, TestDatabaseHub testHub) {
            testHub.ClearErrors();
            const string articleError = @"EntityErrors ~ count: 2
| ReadError: articles [article-1], simulated read entity error
| ParseError: articles [article-2], JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16";
            
            TestContainer testArticles = testHub.GetTestContainer(nameof(PocStore.articles));
            const string article1ReadError      = "article-1";
            const string article2JsonError      = "article-2";
            const string articleInvalidJson     = "article-invalidJson";
            const string articleIdDoesntMatch   = "article-idDoesntMatch";
            const string articleMissingKey      = "article-missingKey";
            
            testArticles.readEntityErrors.Add(article2JsonError,    new SimJson(@"{""invalidJson"" XXX}"));
            testArticles.readEntityErrors.Add(article1ReadError,    new SimReadError());
            testArticles.readEntityErrors.Add(articleInvalidJson,   new SimJson(@"{""invalidJson"" YYY}"));
            testArticles.readEntityErrors.Add(articleIdDoesntMatch, new SimJson(@"{""id"": ""article-unexpected-id""}"));
            testArticles.readEntityErrors.Add(articleMissingKey,    new SimJson(@"{}"));
            
            var orders      = store.orders;
            var articles    = store.articles;
            var producers   = store.producers;
            var customers   = store.customers;  
            
            var readOrders  = orders.Read();
            var order1Task  = readOrders.Find("order-1");
            await store.SyncTasks();
            
            // schedule ReadRefs on an already synced Read operation
            Exception e;
            var orderCustomer = orders.RelationPath(customers, o => o.customer);
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRelation(customers, orderCustomer); });
            AreEqual("Task already executed. ReadTask<Order> (ids: 1)", e.Message);
            var itemsArticle = orders.RelationsPath(articles, o => o.items.Select(a => a.article));
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRelations(articles, itemsArticle); });
            AreEqual("Task already executed. ReadTask<Order> (ids: 1)", e.Message);
            
            // todo add Read() without ids 

            readOrders              = orders.Read()                                                 .TaskName("readOrders");
            var order1              = readOrders.Find("order-1"); //                                .TaskName("order1");
            var orderArticles       = readOrders.ReadRelations(articles, itemsArticle); //          .TaskName("orderArticles");
            var orderArticles2      = readOrders.ReadRelations(articles, itemsArticle);
            AreSame(orderArticles, orderArticles2);
            
            var orderArticles3      = readOrders.ReadRelations(articles, o => o.items.Select(a => a.article));
            AreSame(orderArticles, orderArticles3);
            AreEqual("readOrders -> .items[*].article", orderArticles.Label);

            e = Throws<TaskNotSyncedException>(() => { var _ = orderArticles.Result; });
            AreEqual("ReadRelations.Result requires SyncTasks(). readOrders -> .items[*].article", e.Message);

            var articleProducer = orderArticles.ReadRelations(producers, a => a.producer); //       .TaskName("articleProducer");
            AreEqual("readOrders -> .items[*].article -> .producer", articleProducer.Label);

            var duplicateId     = "article-galaxy"; // support duplicate ids
            
            var readArticles    = articles.Read()                                                   .TaskName("readArticles");
            var galaxy          = readArticles.Find(duplicateId); //                                .TaskName("galaxy");
            var article1        = readArticles.Find(article1ReadError); //                          .TaskName("article1");
            var article1And2    = readArticles.FindRange(new [] {article1ReadError, article2JsonError}); //.TaskName("article1And2");
            var articleSet      = readArticles.FindRange(new [] {duplicateId, duplicateId, "article-ipad"}); //.TaskName("articleSet");
            
            var readArticles2   = articles.Read(); //                                               .TaskName("readArticles2"); // separate Read without errors
            var galaxy2         = readArticles2.Find(duplicateId); //                               .TaskName("galaxy2");
            
            var readArticles3   = articles.Read()                                                   .TaskName("readArticles3");
            var invalidJson     = readArticles3.Find(articleInvalidJson); //                        .TaskName("invalidJson");
            var idDoesntMatch   = readArticles3.Find(articleIdDoesntMatch); //                      .TaskName("idDoesntMatch");
            var missingKey      = readArticles3.Find(articleMissingKey); //                         .TaskName("missingKey");
            
            // test throwing exception in case of task or entity errors
            SyncTasksException ex = null;
            try {
                AreEqual(4,  store.Tasks.Count);
                await store.SyncTasks(); // ----------------
                
                Fail("SyncTasks() intended to fail - code cannot be reached");
            } catch (SyncTasksException sre) {
                ex = sre;
            }
            AreEqual(2, ex.failed.Count);
            AreEqual(@"SyncTasks() failed with task errors. Count: 2
|- readArticles # EntityErrors ~ count: 2
|   ReadError: articles [article-1], simulated read entity error
|   ParseError: articles [article-2], JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- readArticles3 # EntityErrors ~ count: 3
|   ParseError: articles [article-idDoesntMatch], entity key mismatch. 'id': 'article-unexpected-id'
|   ParseError: articles [article-invalidJson], JsonParser/JSON error: Expected ':' after key. Found: Y path: 'invalidJson' at position: 16
|   ParseError: articles [article-missingKey], missing key in JSON value. keyName: 'id'", ex.Message);
            
            IsTrue(readOrders.Success);
            AreEqual(3, readOrders.Result["order-1"].items.Count);
            // readOrders is successful
            // but resolving its Ref<>'s (.items[*].article and .items[*].article > .producer) failed:
            
            IsFalse(orderArticles.Success);
            AreEqual(articleError, orderArticles.Error.ToString());
            
            IsFalse(articleProducer.Success);
            AreEqual(articleError, articleProducer.Error.ToString());

            // readArticles failed - A ReadTask<> fails, if any FindTask<> of it failed.
            TaskResultException te;
            
            IsFalse(readArticles.Success);
            te = Throws<TaskResultException>(() => { var _ = readArticles.Result; });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            IsTrue(articleSet.Success);
            AreEqual(2,             articleSet.Result.Count);
            AreEqual("Galaxy S10",  articleSet.Result[duplicateId].name);
            AreEqual("iPad Pro",    articleSet.Result["article-ipad"].name);

            IsTrue(galaxy.Success);
            AreEqual("Galaxy S10",  galaxy.Result.name);
            
            IsFalse(article1.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ReadError: articles [article-1], simulated read entity error", article1.Error.ToString());

            AreEqual(@"EntityErrors ~ count: 2
| ReadError: articles [article-1], simulated read entity error
| ParseError: articles [article-2], JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16", article1And2.Error.Message);
            te = Throws<TaskResultException>(() => { var _ = article1And2.Result; });
            AreEqual(articleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            // readArticles2 succeed - All it FindTask<> were successful
            IsTrue(readArticles2.Success);
            IsTrue(galaxy2.Success);
            AreEqual("Galaxy S10", galaxy2.Result.name); 
            
            IsFalse(invalidJson.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ParseError: articles [article-invalidJson], JsonParser/JSON error: Expected ':' after key. Found: Y path: 'invalidJson' at position: 16", invalidJson.Error.ToString());
            
            IsFalse(idDoesntMatch.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ParseError: articles [article-idDoesntMatch], entity key mismatch. 'id': 'article-unexpected-id'", idDoesntMatch.Error.ToString());
            
            IsFalse(missingKey.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ParseError: articles [article-missingKey], missing key in JSON value. keyName: 'id'", missingKey.Error.ToString());
            
        }
    }
}