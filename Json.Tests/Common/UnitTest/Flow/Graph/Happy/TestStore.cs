// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Remote;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public class TestStore : LeakTestsFixture
    {
        /// withdraw from allocation detection by <see cref="LeakTestsFixture"/> => init before tracking starts
        static TestStore() { SyncTypeStore.Init(); }
        

        [UnityTest] public IEnumerator  CollectAwaitCoroutine() { yield return RunAsync.Await(CollectAwait(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task   CollectAwaitAsync() { await CollectAwait(); }
        
        private async Task CollectAwait() {
            List<Task> tasks = new List<Task>();
            for (int n = 0; n < 1000; n++) {
                Task task = Task.Delay(1);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }

        [UnityTest] public IEnumerator  ChainAwaitCoroutine() { yield return RunAsync.Await(ChainAwait(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task   ChainAwaitAsync() { await ChainAwait(); }
        private async Task ChainAwait() {
            for (int n = 0; n < 5; n++) {
                await Task.Delay(1);
            }
        }
        
        [UnityTest] public IEnumerator  MemoryCreateCoroutine() { yield return RunAsync.Await(MemoryCreate()); }
        [Test]      public async Task   MemoryCreateAsync() { await MemoryCreate(); }
        
        private async Task MemoryCreate() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var database     = new MemoryDatabase())
            using (var createStore  = await TestRelationPoC.CreateStore(database))
            using (var useStore     = new PocStore(database))  {
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator FileCreateCoroutine() { yield return RunAsync.Await(FileCreate(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  FileCreateAsync() { await FileCreate(); }

        private async Task FileCreate() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var createStore  = await TestRelationPoC.CreateStore(fileDatabase))
            using (var useStore     = new PocStore(fileDatabase)) {
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator FileUseCoroutine() { yield return RunAsync.Await(FileUse()); }
        [Test]      public async Task  FileUseAsync() { await FileUse(); }
        
        private async Task FileUse() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var createStore  = new PocStore(fileDatabase))
            using (var useStore     = new PocStore(fileDatabase)) {
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator HttpCreateCoroutine() { yield return RunAsync.Await(HttpCreate()); }
        [Test]      public async Task  HttpCreateAsync() { await HttpCreate(); }
        
        private async Task HttpCreate() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var hostDatabase = new HttpHostDatabase(fileDatabase, "http://+:8080/")) {
                await RunRemoteHost(hostDatabase, async () => {
                    using (var remoteDatabase   = new HttpClientDatabase("http://localhost:8080/"))
                    using (var createStore      = await TestRelationPoC.CreateStore(remoteDatabase))
                    using (var useStore         = new PocStore(remoteDatabase)) {
                        await TestStores(createStore, useStore);
                    }
                });
            }
        }
        
        [UnityTest] public IEnumerator LoopbackUseCoroutine() { yield return RunAsync.Await(LoopbackUse()); }
        [Test]      public async Task  LoopbackUseAsync() { await LoopbackUse(); }
        
        private async Task LoopbackUse() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var loopbackDatabase = new LoopbackDatabase(fileDatabase)) {
                using (var createStore      = new PocStore(loopbackDatabase))
                using (var useStore         = new PocStore(loopbackDatabase)) {
                    await TestStores(createStore, useStore);
                }
            }
        }
        
        internal static async Task RunRemoteHost(HttpHostDatabase remoteHost, Func<Task> run) {
            remoteHost.Start();
            var hostTask = Task.Run(() => {
                // await hostDatabase.HandleIncomingConnections();
                remoteHost.Run();
                // await Task.Delay(100); // test awaiting hostTask
                Logger.Info("1. RemoteHost finished");
            });
            
            await run();
            
            await remoteHost.Stop();
            await hostTask;
            Logger.Info("2. awaited hostTask");
        } 

        // ------------------------------------ test assertion methods ------------------------------------
        private static async Task TestStores(PocStore createStore, PocStore useStore) {
            await WriteRead             (createStore);
            await AssertEntityIdentity  (createStore);
            await AssertQueryTask       (createStore);
            await AssertReadTask        (createStore);
            await AssertRefAssignment   (useStore);
        }

        private static async Task AssertRefAssignment(PocStore store) {
            var articles    = store.articles;
            var producers   = store.producers;

            var readArticles    = articles.Read();
            var galaxyTask      = readArticles.Find("article-galaxy"); // entity exist in database 
            await store.Sync();  // -------- Sync --------

            var galaxy = galaxyTask.Result;
            // the referenced entity "producer-samsung" is not resolved until now.
            Exception e;
            e = Throws<UnresolvedRefException>(() => { var _ = galaxy.producer.Entity; });
            AreEqual("Accessed unresolved reference. Ref<Producer> (id: 'producer-samsung')", e.Message);
            IsFalse(galaxy.producer.TryEntity(out Producer result));
            IsNull(result);
            var readProducers = producers.Read();
            galaxy.producer.FindBy(readProducers); // schedule resolving producer reference now
            
            // assign producer field with id "producer-apple"
            var iphone = new Article  { id = "article-iphone", name = "iPhone 11", producer = "producer-apple" };
            iphone.producer.FindBy(readProducers);
            
            var tesla  = new Producer { id = "producer-tesla", name = "Tesla" };
            // assign producer field with entity instance tesla
            var model3 = new Article  { id = "article-model3", name = "Model 3", producer = tesla };
            IsTrue(model3.producer.TryEntity(out result));
            AreSame(tesla, result);
            
            AreEqual("Tesla",   model3.producer.Entity.name);   // Entity is directly accessible

            await store.Sync();  // -------- Sync --------
            
            AreEqual("Samsung", galaxy.producer.Entity.name);   // after Sync() Entity is accessible
            AreEqual("Apple",   iphone.producer.Entity.name);   // after Sync() Entity is accessible
        }

        private static bool lab = false;

        private static async Task AssertQueryTask(PocStore store) {
            var orders = store.orders;
            var articles = store.articles;

            var readOrders              = orders.Read()                                             .TaskName("readOrders");
            var order1                  = readOrders.Find("order-1")                                .TaskName("order1");
            AreEqual("Find<Order> (id: 'order-1')", order1.Details); 
            var allArticles             = articles.QueryAll()                                       .TaskName("allArticles");
            var filterAll               = new EntityFilter<Article>(a => true); 
            var allArticles2            = articles.QueryByFilter(filterAll)                         .TaskName("allArticles2");
            var producersTask           = allArticles.ReadRefs(a => a.producer);
            var hasOrderCamera          = orders.Query(o => o.items.Any(i => i.name == "Camera"))   .TaskName("hasOrderCamera");
            var ordersWithCustomer1     = orders.Query(o => o.customer.id == "customer-1")          .TaskName("ordersWithCustomer1");
            var read3                   = orders.Query(o => o.items.Count(i => i.amount < 1) > 0)   .TaskName("read3");
            var ordersAnyAmountLowerFilter = new EntityFilter<Order>(o => o.items.Any(i => i.amount < 2));
            var ordersAnyAmountLower2   = orders.QueryByFilter(ordersAnyAmountLowerFilter)          .TaskName("ordersAnyAmountLower2");
            var ordersAllAmountGreater0 = orders.Query(o => o.items.All(i => i.amount > 0))         .TaskName("ordersAllAmountGreater0");

            var orderCustomer           = orders.RefPath(o => o.customer);
            var customer                = readOrders.ReadRefPath(orderCustomer);
            var customer2               = readOrders.ReadRefPath(orderCustomer);
            AreSame(customer, customer2);
            var customer3               = readOrders.ReadRef(o => o.customer);
            AreSame(customer, customer3);
            AreEqual("readOrders -> .customer", customer.Details);

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
            AreEqual("allArticles2 -> .producer -> .employees[*]", producerEmployees.ToString());

            // lab - test ReadRef expressions
            if (lab) {
                readOrders.ReadRefsOfType<Article>();
                readOrders.ReadAllRefs();
            }

            await store.Sync(); // -------- Sync --------
            AreEqual(1,                 ordersWithCustomer1.Results.Count);
            NotNull(ordersWithCustomer1["order-1"]);
            
            AreEqual(1,                 ordersAnyAmountLower2.Results.Count);
            NotNull(ordersAnyAmountLower2["order-1"]);
            
            AreEqual(2,                 ordersAllAmountGreater0.Results.Count);
            NotNull(ordersAllAmountGreater0["order-1"]);

            AreEqual(6,                 allArticles.Results.Count);
            AreEqual("Galaxy S10",      allArticles.Results["article-galaxy"].name);
            AreEqual("iPad Pro",        allArticles.Results["article-ipad"].name);
            
            AreEqual(1,                 hasOrderCamera.Results.Count);
            AreEqual(3,                 hasOrderCamera["order-1"].items.Count);
    
            AreEqual("customer-1",      customer.Id);
            AreEqual("Smith Ltd.",      customer.Result.name);
                
            AreEqual(3,                 producersTask.Results.Count);
            AreEqual("Samsung",         producersTask["producer-samsung"].name);
            AreEqual("Canon",           producersTask["producer-canon"].name);
            AreEqual("Apple",           producersTask["producer-apple"].name);
                
            AreEqual(1,                 producerEmployees.Results.Count);
            AreEqual("Steve",           producerEmployees["apple-0001"].firstName);
        }

        /// Optimization: <see cref="RefPath{TEntity,TRef}"/> and <see cref="RefsPath{TEntity,TRef}"/> can be created static as creating
        /// a path from a <see cref="System.Linq.Expressions.Expression"/> is costly regarding heap allocations and CPU.
        private static readonly RefPath<Order, Customer> OrderCustomer = RefPath<Order, Customer>.MemberRef(o => o.customer);
        private static readonly RefsPath<Order, Article> ItemsArticle  = RefsPath<Order, Article>.MemberRefs(o => o.items.Select(a => a.article));
        
        private static async Task AssertReadTask(PocStore store) {
            var orders = store.orders;
            var readOrders = orders.Read()                                      .TaskName("readOrders");
            var order1Task = readOrders.Find("order-1")                         .TaskName("order1Task");
            await store.Sync();
            
            // schedule ReadRefs on an already synced Read operation
            Exception e;
            var orderCustomer = orders.RefPath(o => o.customer);
            AreEqual(OrderCustomer.path, orderCustomer.path);
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefPath(orderCustomer); });
            AreEqual("Task already synced. readOrders", e.Message);
            var itemsArticle = orders.RefsPath(o => o.items.Select(a => a.article));
            AreEqual(ItemsArticle.path, itemsArticle.path);
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefsPath(itemsArticle); });
            AreEqual("Task already synced. readOrders", e.Message);
            
            // todo add Read() without ids 

            readOrders              = orders.Read()                                             .TaskName("readOrders");
            var order1              = readOrders.Find("order-1")                                .TaskName("order1");
            var articleRefsTask     = readOrders.ReadRefsPath(itemsArticle);
            var articleRefsTask2    = readOrders.ReadRefsPath(itemsArticle);
            AreSame(articleRefsTask, articleRefsTask2);
            
            var articleRefsTask3 = readOrders.ReadArrayRefs(o => o.items.Select(a => a.article));
            AreSame(articleRefsTask, articleRefsTask3);
            AreEqual("readOrders -> .items[*].article", articleRefsTask.Details);

            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask["article-1"]; });
            AreEqual("ReadRefsTask[] requires Sync(). readOrders -> .items[*].article", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask.Results; });
            AreEqual("ReadRefsTask.Results requires Sync(). readOrders -> .items[*].article", e.Message);

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

            await store.Sync(); // -------- Sync --------
        
            AreEqual(2,                 articleRefsTask.Results.Count);
            AreEqual("Changed name",    articleRefsTask["article-1"].name);
            AreEqual("Smartphone",      articleRefsTask["article-2"].name);
            
            AreEqual(1,                 articleProducerTask.Results.Count);
            AreEqual("Canon",           articleProducerTask["producer-canon"].name);

            AreEqual(2,                 articleSet.Results.Count);
            AreEqual("Galaxy S10",      articleSet["article-galaxy"].name);
            AreEqual("iPad Pro",        articleSet["article-ipad"].name);
            
            AreEqual("Galaxy S10",      galaxy.Result.name);
            
            AreEqual(2,                 article1And2.Results.Count);
            AreEqual("Smartphone",      article1And2["article-2"].name);
            
            AreEqual(4,                 readTask.Results.Count);
            AreEqual("Galaxy S10",      readTask["article-galaxy"].name);
        }
        
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
        
        private static async Task WriteRead(PocStore createStore) {
            // --- cache empty
            var readOrders  = createStore.orders.Read();
            var orderTask   = readOrders.Find("order-1");
            await createStore.Sync();

            var order = orderTask.Result;
            using (ObjectMapper mapper = new ObjectMapper(createStore.TypeStore)) {
                mapper.Pretty = true;
            
                AssertWriteRead(mapper, order);
                AssertWriteRead(mapper, order.customer);
                AssertWriteRead(mapper, order.items[0]);
                AssertWriteRead(mapper, order.items[1]);
                AssertWriteRead(mapper, order.items[0].article);
                AssertWriteRead(mapper, order.items[1].article);
            }
        }
        
        private static void AssertWriteRead<T>(ObjectMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            // IsTrue(entity.Equals(result)); // references are equal
        }
    }
}