// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.AssertUtils;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public static class TestRelationPoC
    {
        public static async Task CreateStore(PocStore store) {
            
            AreSimilar("entities: 0",    store);    // initial state, empty store
            var orders      = store.orders;
            var articles    = store.articles;
            var producers   = store.producers;
            var employees   = store.employees;
            var customers   = store.customers;
            var types       = store.types;
            store.types.WriteNull = true;
            
            // delete stored (persisted) entities to enable creation below
            // Create() entity will fail, if already stored (persisted) 
            producers.Delete("producer-samsung");
            producers.Delete("producer-apple");
            producers.Delete("producer-canon");
            articles. Delete("article-1");
            articles. Delete("article-2");
            employees.Delete("apple-0001");
            customers.Delete("customer-1");
            await store.SyncTasks();

            var samsung         = new Producer { id = "producer-samsung", name = "Samsung"};
            producers.Create(samsung);
            var samsungJson     = samsung.ToString();
            AreEqual("{\"id\":\"producer-samsung\",\"name\":\"Samsung\"}", samsungJson);
            
            var galaxy          = new Article  { id = "article-galaxy",   name = "Galaxy S10", producer = samsung.id};
            var createGalaxy    = articles.Upsert(galaxy);
            AreSimilar("entities: 2, tasks: 2",                         store);
            AreSimilar("articles: 1, tasks: 1 >> upsert #1",            articles);

            var storePatches = store.DetectAllPatches();
            AreEqual(0, storePatches.PatchCount);
            
            AreSimilar("entities:  2, tasks: 2",                         store);
            AreSimilar("producers: 1, tasks: 1 >> create #1",            producers);

            var steveJobs       = new Employee { id = "apple-0001", firstName = "Steve", lastName = "Jobs"};
            employees.Create(steveJobs);
            var appleEmployees  = new List<string>{ steveJobs.id };
            var apple           = new Producer { id = "producer-apple", name = "Apple", employeeList = appleEmployees};
            producers.Create(apple);
            var ipad            = new Article  { id = "article-ipad",   name = "iPad Pro", producer = apple.id};
            var createIPad      = articles.Upsert(ipad);
            AreSimilar("articles: 2, tasks: 1 >> upsert #2",                        articles);
            
            var logStore2 = store.DetectAllPatches();
            AreEqual(0, logStore2.PatchCount);
            AreSimilar("entities:  5, tasks: 3",                                     store);
            AreSimilar("articles:  2, tasks: 1 >> upsert #2",                        articles);
            AreSimilar("employees: 1, tasks: 1 >> create #1",                        employees);
            AreSimilar("producers: 2, tasks: 1 >> create #2",                        producers);

            await store.SyncTasks(); // ----------------
            AreSimilar("entities: 5",                                   store);   // tasks executed and cleared
            
            IsTrue(storePatches.Success);
            IsTrue(logStore2.Success);
            IsTrue(createGalaxy.Success);
            IsTrue(createIPad.Success);
            IsNull(createIPad.Error); // error is null if successful
            
            var canon           = new Producer { id = "producer-canon", name = "Canon"};
            var createCanon     = producers.Create(canon);
            var order           = new Order { id = "order-1", created = new DateTime(2021, 7, 22, 6, 0, 0, DateTimeKind.Utc)};
            var cameraCreate    = new Article { id = "article-1", name = "Camera", producer = canon.id };
            var notebook        = new Article { id = "article-notebook-💻-unicode", name = "Notebook", producer = samsung.id };
            var derivedClass    = new DerivedClass{ article = cameraCreate.id };
            var type1           = new TestType { id = "type-1", dateTime = new DateTime(2021, 7, 22, 6, 0, 0, DateTimeKind.Utc), derivedClass = derivedClass };
            var createCam1      = articles.Create(cameraCreate);
                                  articles.Upsert(notebook);
            var createCam2      = articles.Create(cameraCreate);   // Create new CreateTask for same entity
            AreNotSame(createCam1, createCam2);               
            AreEqual("CreateTask<Article> (#keys: 1)", createCam1.ToString());

            var newBulkArticles = new List<Article>();
            for (int n = 0; n < 2; n++) {
                var id = $"bulk-article-{n:D4}";
                var newArticle = new Article { id = id, name = id };
                newBulkArticles.Add(newArticle);
            }
            articles.UpsertRange(newBulkArticles);

            var readArticles    = articles.Read();
            var cameraUnknown   = readArticles.Find("article-missing");
            var camera          = readArticles.Find("article-1");
            
            var camForDelete    = new Article { id = "article-delete", name = "Camera-Delete" };
            articles.Upsert(camForDelete);
            // StoreInfo is accessible via property an ToString()
            AreEqual(11, store.StoreInfo.peers);
            AreEqual(4,  store.StoreInfo.tasks); 
            AreSimilar("entities: 11, tasks: 4",                                    store);
            AreSimilar("articles:  7, tasks: 3 >> create #1, upsert #4, reads: 1",  articles);
            AreSimilar("producers: 3, tasks: 1 >> create #1",                       producers);
            AreSimilar("employees: 1",                                              employees);
            
            await store.SyncTasks(); // ----------------
            AreSimilar("entities: 12",                                  store); // tasks cleared
            
            IsTrue(createCam1.Success);
            IsTrue(createCanon.Success);
            

            articles.DeleteRange(newBulkArticles);
            AreSimilar("entities: 12, tasks: 1",                        store);
            AreSimilar("articles:  8, tasks: 1 >> delete #2",           articles);
            
            await store.SyncTasks(); // ----------------
            AreSimilar("entities: 10",                                  store); // tasks cleared
            AreSimilar("articles:  6",                                  articles);

            cameraCreate.name = "Changed name";
            var entityPatches   = articles.DetectEntityPatches(cameraCreate);
            AreEqual(1, entityPatches.Patches.Count);
            
            var articlePatches  = articles.DetectPatches();
            AreEqual(1, articlePatches.Patches.Count);
            AreEqual("DetectPatchesTask (container: articles, patches: 1)", articlePatches.ToString());

            var storePatches3 = store.DetectAllPatches();
            AreEqual(1, storePatches3.PatchCount);
            
            var storePatches4 = store.DetectAllPatches();
            AreEqual(1, storePatches4.PatchCount);


            var deleteCamera = articles.Delete(camForDelete.id);
            
            await store.SyncTasks(); // ----------------
            
            IsTrue(entityPatches.Success);
            IsTrue(articlePatches.Success);
            IsTrue(storePatches3.Success);
            IsTrue(storePatches4.Success);
            IsTrue(deleteCamera.Success);
            AreSimilar("entities: 9",                           store);       // tasks executed and cleared

            AreSimilar("articles: 5",                           articles);
            var readArticles2   = articles.Read();
            var cameraNotSynced = readArticles2.Find("article-1");
            AreSimilar("entities: 9, tasks: 1",                 store);
            AreSimilar("articles: 5, tasks: 1 >> reads: 1",     articles);
            
            var e = Throws<TaskNotSyncedException>(() => { var _ = cameraNotSynced.Result; });
            AreSimilar("Find.Result requires SyncTasks(). Find<Article> (id: 'article-1')", e.Message);
            
            IsNull(cameraUnknown.Result);
            AreSame(camera.Result, cameraCreate);
            
            var customer    = new Customer { id = "customer-1", name = "Smith Ltd." };
            customers.Create(customer);
            
            var smartphone  = new Article { id = "article-2", name = "Smartphone" };
            articles.Create(smartphone);
            
            var item1 = new OrderItem { article = camera.Result.id, amount = 1, name = "Camera" };
            var item2 = new OrderItem { article = smartphone.id,    amount = 2, name = smartphone.name };
            var item3 = new OrderItem { article = camera.Result.id, amount = 3, name = "Camera" };
            order.items.AddRange(new [] { item1, item2, item3 });
            order.customer = customer.id;
            
            AreSimilar("entities: 11, tasks: 3",                        store);
            
            AreSimilar("orders:    0",                                  orders);
            orders.Upsert(order);
            types.Upsert(type1);
            AreSimilar("entities: 13, tasks: 5",                        store);
            AreSimilar("orders:    1, tasks: 1 >> upsert #1",           orders);     // created order
            
            AreSimilar("articles:  6, tasks: 2 >> create #1, reads: 1", articles);
            AreSimilar("customers: 1, tasks: 1 >> create #1",           customers);
            var orderPatches = orders.DetectPatches();
            AreEqual(0, orderPatches.Patches.Count);
            AreSimilar("entities: 13, tasks: 5",                        store);
            AreSimilar("articles:  6, tasks: 2 >> create #1, reads: 1", articles);
            AreSimilar("customers: 1, tasks: 1 >> create #1",           customers);
            
            AreSimilar("entities: 13, tasks: 5",                        store);
            var storePatches1 = store.DetectAllPatches();
            AreEqual(0, storePatches1.PatchCount);
            
            var storePatches2 = store.DetectAllPatches();
            AreEqual(0, storePatches2.PatchCount);
            
            AreSimilar("entities: 13, tasks: 5",                        store);      // no new changes

            await store.SyncTasks(); // ----------------
            
            IsTrue(orderPatches.Success);
            IsTrue(storePatches1.Success);
            IsTrue(storePatches2.Success);
            AreSimilar("entities: 13",                                  store);      // tasks executed and cleared
            
            
            // patch the same article with two Patch methods   
            notebook.name = "Galaxy Book";
            var patchNotebook = articles.Patch(member => member.Add(a => a.name));
            patchNotebook.Add(notebook);
            var producerPath = new MemberPath<Article>(a => a.producer);
            var patchArticles = articles.Patch(member => member.Add(producerPath));
            patchArticles.Add(notebook);
            
            var patches = patchNotebook.Patches;
            AreEqual(1, patches.Count);
            AreEqual("article-notebook-💻-unicode", patches[0].Id.AsString());
            var patchMembers = patches[0].Members;
            AreEqual(2, patchMembers.Count);
            AreEqual(".name",     patchMembers[0].path); AreEqual("\"Galaxy Book\"",      patchMembers[0].value.AsString());
            AreEqual(".producer", patchMembers[1].path); AreEqual("\"producer-samsung\"", patchMembers[1].value.AsString());
            
            AreEqual(".producer",                                           producerPath.ToString());
            AreEqual("PatchTask<Article> patches: 1, members: [.name]",     patchNotebook.ToString());
            AreEqual("PatchTask<Article> patches: 1, members: [.producer]", patchArticles.ToString());
            
            AreSimilar("articles:  6, tasks: 1 >> patch #1",            articles);
            AreSimilar("entities: 13, tasks: 1",                        store);      // tasks executed and cleared
            
            await store.SyncTasks(); // ----------------
            AreSimilar("entities: 13",                                  store);      // tasks executed and cleared
            
            IsTrue(patchNotebook.Success);
            IsTrue(patchArticles.Success);

            customers.Upsert(new Customer{id = "log-patch-entity-read-error",   name = "used for successful read"});
            customers.Upsert(new Customer{id = "log-patch-entity-write-error",  name = "used for successful read"});
            
            customers.Upsert(new Customer{id = "patch-task-error",              name = "used for successful patch-read"});
            customers.Upsert(new Customer{id = "patch-task-exception",          name = "used for successful patch-read"});
            customers.Upsert(new Customer{id = "patch-write-entity-error",      name = "used for successful patch-read"});
            customers.Upsert(new Customer{id = "customer-2",                    name = "Armstrong Inc."});

            articles.Upsert (new Article {id = "log-create-read-error",         name = "used for successful read"});
            await store.SyncTasks();
            
            var errorRefTask = new Customer{ id = "read-task-error" };
            var order2 = new Order{id = "order-2", customer = errorRefTask.id, created = new DateTime(2021, 7, 22, 6, 1, 0, DateTimeKind.Utc)};
            orders.Upsert(order2);
            
            var testMessage = new TestCommand{ text = "test message" };
            var sendMessage1 = store.Test(testMessage);
            int testMessageInt = 42;
            var sendMessage2 = store.SendMessage(TestMessageInt,        testMessageInt);
            var sendMessage3 = store.SendMessage(TestRemoveHandler,     1337);
            var sendMessage4 = store.SendMessage(TestRemoveAllHandler,  1337);
            store.SendMessage(EndCreate);  // indicates store changes are finished
            
            await store.SyncTasks(); // ----------------
            
            AreEqual(true, sendMessage1.Result);
            IsTrue(sendMessage1.Success);
            IsTrue(sendMessage2.Success);
            IsTrue(sendMessage3.Success);
            IsTrue(sendMessage4.Success);
        }

        internal const string EndCreate             = "EndCreate";
        internal const string TestMessageInt        = "TestMessageInt";
        internal const string TestRemoveHandler     = "TestRemoveHandler";
        internal const string TestRemoveAllHandler  = "TestRemoveAllHandler";
    }
}