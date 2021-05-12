using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Remote;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Transform;
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
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestStore : LeakTestsFixture
    {
        /// withdraw from allocation detection ny <see cref="LeakTestsFixture"/> by creating before tracking starts
        [OneTimeSetUp]
        public void OneTimeSetUp() { SyncTypeStore.Init(); }
        

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
            using (var database     = new MemoryDatabase())
            using (var createStore  = await TestRelationPoC.CreateStore(database))
            using (var useStore     = new PocStore(database))  {
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator FileCreateCoroutine() { yield return RunAsync.Await(FileCreate(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  FileCreateAsync() { await FileCreate(); }

        private async Task FileCreate() {
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var createStore  = await TestRelationPoC.CreateStore(fileDatabase))
            using (var useStore     = new PocStore(fileDatabase)) {
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator FileUseCoroutine() { yield return RunAsync.Await(FileUse()); }
        [Test]      public async Task  FileUseAsync() { await FileUse(); }
        
        private async Task FileUse() {
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var createStore  = new PocStore(fileDatabase))
            using (var useStore     = new PocStore(fileDatabase)) {
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator HttpCreateCoroutine() { yield return RunAsync.Await(HttpCreate()); }
        [Test]      public async Task  HttpCreateAsync() { await HttpCreate(); }
        
        private async Task HttpCreate() {
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
            AreEqual("Accessed unresolved reference. Ref<Producer> id: producer-samsung", e.Message);
            IsFalse(galaxy.producer.TryEntity(out Producer result));
            IsNull(result);
            var readProducers = producers.Read();
            galaxy.producer.ReadBy(readProducers); // schedule resolving producer reference now
            
            // assign producer field with id "producer-apple"
            var iphone = new Article  { id = "article-iphone", name = "iPhone 11", producer = "producer-apple" };
            iphone.producer.ReadBy(readProducers);
            
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

            var readOrders  = orders.Read();
            var order1      = readOrders.Find("order-1");
            AreEqual("ReadId<Order> id: order-1", order1.ToString());
            var allArticles             = articles.QueryAll();
            var allArticles2            = articles.QueryByFilter(Operation.FilterTrue);
            var producersTask           = allArticles.ReadRefs(a => a.producer);
            var hasOrderCamera          = orders.Query(o => o.items.Any(i => i.name == "Camera"));
            var ordersWithCustomer1     = orders.Query(o => o.customer.id == "customer-1");
            var read3                   = orders.Query(o => o.items.Count(i => i.amount < 1) > 0);
            var ordersAnyAmountLower2   = orders.Query(o => o.items.Any(i => i.amount < 2));
            var ordersAllAmountGreater0 = orders.Query(o => o.items.All(i => i.amount > 0));

            ReadRefTask<Customer> customer  = readOrders.ReadRefByPath<Customer>(".customer");
            ReadRefTask<Customer> customer2 = readOrders.ReadRefByPath<Customer>(".customer");
            AreSame(customer, customer2);
            ReadRefTask<Customer> customer3 = readOrders.ReadRef(o => o.customer);
            AreSame(customer, customer3);
            AreEqual("ReadTask<Order> #ids: 1 > .customer", customer.ToString());

            Exception e;
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Id; });
            AreEqual("ReadRefTask.Id requires Sync(). ReadTask<Order> #ids: 1 > .customer", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Result; });
            AreEqual("ReadRefTask.Result requires Sync(). ReadTask<Order> #ids: 1 > .customer", e.Message);

            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera.Results; });
            AreEqual("QueryTask.Result requires Sync(). QueryTask<Order> filter: .items.Any(i => i.name == 'Camera')", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera["arbitrary"]; });
            AreEqual("QueryTask[] requires Sync(). QueryTask<Order> filter: .items.Any(i => i.name == 'Camera')", e.Message);

            var producerEmployees = producersTask.ReadArrayRefs(p => p.employeeList);
            AreEqual("QueryTask<Article> filter: true > .producer > .employees[*]", producerEmployees.ToString());

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
            
            AreEqual(1,                 ordersAllAmountGreater0.Results.Count);
            NotNull(ordersAllAmountGreater0["order-1"]);

            AreEqual(4,                 allArticles.Results.Count);
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
        
        private static async Task AssertReadTask(PocStore store) {
            var orders = store.orders;
            var readOrders = orders.Read();
            var order1Task = readOrders.Find("order-1");
            await store.Sync();
            
            // schedule ReadRefs on an already synced Read operation
            Exception e;
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefByPath<Article>("customer"); });
            AreEqual("Task already synced. ReadTask<Order> #ids: 1", e.Message);
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefsByPath<Article>("items[*].article"); });
            AreEqual("Task already synced. ReadTask<Order> #ids: 1", e.Message);
            
            // todo add Read() without ids 

            readOrders = orders.Read();
            readOrders.Find("order-1");
            ReadRefsTask<Article> articleRefsTask  = readOrders.ReadRefsByPath<Article>(".items[*].article");
            ReadRefsTask<Article> articleRefsTask2 = readOrders.ReadRefsByPath<Article>(".items[*].article");
            AreSame(articleRefsTask, articleRefsTask2);
            
            ReadRefsTask<Article> articleRefsTask3 = readOrders.ReadArrayRefs(o => o.items.Select(a => a.article));
            AreSame(articleRefsTask, articleRefsTask3);
            AreEqual("ReadTask<Order> #ids: 1 > .items[*].article", articleRefsTask.ToString());

            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask["article-1"]; });
            AreEqual("ReadRefsTask[] requires Sync(). ReadTask<Order> #ids: 1 > .items[*].article", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask.Results; });
            AreEqual("ReadRefsTask.Results requires Sync(). ReadTask<Order> #ids: 1 > .items[*].article", e.Message);

            ReadRefsTask<Producer> articleProducerTask = articleRefsTask.ReadRefs(a => a.producer);
            AreEqual("ReadTask<Order> #ids: 1 > .items[*].article > .producer", articleProducerTask.ToString());

            var readTask        = store.articles.Read();
            var duplicateId     = "article-galaxy"; // support duplicate ids
            var galaxy          = readTask.Find(duplicateId);
            var article1And2    = readTask.FindRange(new [] {"article-1", "article-2"});
            var articleSet      = readTask.FindRange(new [] {duplicateId, duplicateId, "article-ipad"});

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
            AreEqual("Task already synced. ReadTask<Order> #ids: 1", e.Message);

            var readOrders2 = orders.Read();
            var order1Task  = readOrders2.Find("order-1");

            var readArticles = articles.Read();
            var article1Task            =  readArticles.Find("article-1");
            // var article1TaskRedundant   =  readArticles.ReadId("article-1");
            // AreSame(article1Task, article1TaskRedundant);
            
            var readCustomers = customers.Read();
            var article2Task =  readArticles.Find("article-2");
            var customer1Task = readCustomers.Find("customer-1");
            var unknownTask   = readCustomers.Find("customer-missing");

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