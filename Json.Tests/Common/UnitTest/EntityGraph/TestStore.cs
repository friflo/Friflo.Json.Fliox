using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.EntityGraph;
using Friflo.Json.EntityGraph.Database;
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


namespace Friflo.Json.Tests.Common.UnitTest.EntityGraph
{
    public class TestStore : LeakTestsFixture
    {
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
        
        [UnityTest] public IEnumerator FileEmptyCoroutine() { yield return RunAsync.Await(FileEmpty()); }
        [Test]      public async Task  FileEmptyAsync() { await FileEmpty(); }
        
        private async Task FileEmpty() {
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var createStore  = new PocStore(fileDatabase))
            using (var useStore     = new PocStore(fileDatabase)) {
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator RemoteCreateCoroutine() { yield return RunAsync.Await(RemoteCreate()); }
        [Test]      public async Task  RemoteCreateAsync() { await RemoteCreate(); }
        
        private async Task RemoteCreate() {
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var hostDatabase = new RemoteHost(fileDatabase, "http://+:8080/")) {
                await RunRemoteHost(hostDatabase, async () => {
                    using (var remoteDatabase   = new RemoteClient("http://localhost:8080/"))
                    using (var createStore      = await TestRelationPoC.CreateStore(remoteDatabase))
                    using (var useStore         = new PocStore(remoteDatabase)) {
                        await TestStores(createStore, useStore);
                    }
                });
            }
        }
        
        private static async Task RunRemoteHost(RemoteHost remoteHost, Func<Task> run) {
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
            
            var galaxyTask = articles.Read("article-galaxy"); // entity exist in database 
            await store.Sync();  // -------- Sync --------

            var galaxy = galaxyTask.Result;
            // the referenced entity "producer-samsung" is not resolved until now.
            Exception e;
            e = Throws<UnresolvedRefException>(() => { var _ = galaxy.producer.Entity; });
            AreEqual("Accessed an unresolved entity. Ref<Producer> id: producer-samsung", e.Message);
            IsFalse(galaxy.producer.TryEntity(out Producer result));
            IsNull(result);

            galaxy.producer.ReadFrom(producers); // schedule resolving producer reference now
            
            // assign producer field with id "producer-apple"
            var iphone = new Article  { id = "article-iphone", name = "iPhone 11", producer = "producer-apple" };
            iphone.producer.ReadFrom(producers);
            
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

            ReadTask<Order> order1 = orders.Read("order-1");
            AreEqual("order-1", order1.ToString());
            var allArticles =  articles.QueryAll();
            var allArticles2 = articles.QueryByFilter(Operation.FilterTrue);
            var producersTask = allArticles.SubRef(a => a.producer);
            var hasOrderCamera = orders.Query(o => o.items.Any(i => i.name == "Camera"));
            var read1 = orders.Query(o => o.customer.id == "customer-1");
            var read2 = orders.Query(o => o.customer.Entity.lastName == "Smith");
            var read3 = orders.Query(o => o.items.Count(i => i.amount < 1) > 0);
            var read4 = orders.Query(o => o.items.Any(i => i.amount < 1));
            var read5 = orders.Query(o => o.items.All(i => i.amount < 1));
            var read6 = orders.Query(o => o.items.Any(i => i.article.Entity.name == "Smartphone"));


            ReadRefTask<Customer> customer  = order1.ReadRefByPath<Customer>(".customer");
            ReadRefTask<Customer> customer2 = order1.ReadRefByPath<Customer>(".customer");
            AreSame(customer, customer2);
            ReadRefTask<Customer> customer3 = order1.ReadRef(o => o.customer);
            AreSame(customer, customer3);
            AreEqual("Order['order-1'] .customer", customer.ToString());

            Exception e;
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Id; });
            AreEqual("ReadRefTask.Id requires Sync(). Order['order-1'] .customer", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Result; });
            AreEqual("ReadRefTask.Result requires Sync(). Order['order-1'] .customer", e.Message);

            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera.Result; });
            AreEqual("QueryTask.Result requires Sync(). Entity: Order filter: .items.Any(i => i.name == 'Camera')", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera[0]; });
            AreEqual("QueryTask[] requires Sync(). Entity: Order filter: .items.Any(i => i.name == 'Camera')", e.Message);

            // lab - test ReadRef expressions
            if (lab) {
                ReadRefsTask<Article> articles2 = order1.ReadRefsOfType<Article>();
                ReadRefsTask<Entity> allDeps = order1.ReadAllRefs();
            }

            await store.Sync(); // -------- Sync --------

            AreEqual(4,             allArticles.Result.Count);
            AreEqual(1,             hasOrderCamera.Result.Count);
            AreEqual("order-1",     hasOrderCamera[0].id);

            AreEqual("customer-1",  customer.Id);
            AreEqual("Smith",       customer.Result.lastName);
            
            AreEqual(1,             producersTask.Results.Count);
            AreEqual("Samsung",     producersTask.Results["producer-samsung"].name);
        }
        
        private static async Task AssertReadTask(PocStore store) {
            var orders = store.orders;
            ReadTask<Order> order1Task = orders.Read("order-1");
            await store.Sync();
            
            // schedule ReadRefs on an already synced Read operation
            Exception e;
            e = Throws<InvalidOperationException>(() => { order1Task.ReadRefByPath<Article>("customer"); });
            AreEqual("Used ReadTask is already synced. ReadTask<Order>, id: order-1", e.Message);
            e = Throws<InvalidOperationException>(() => { order1Task.ReadRefsByPath<Article>("items[*].article"); });
            AreEqual("Used ReadTask is already synced. ReadTask<Order>, id: order-1", e.Message);

            order1Task = orders.Read("order-1");
            ReadRefsTask<Article> articleRefsTask  = order1Task.ReadRefsByPath<Article>(".items[*].article");
            ReadRefsTask<Article> articleRefsTask2 = order1Task.ReadRefsByPath<Article>(".items[*].article");
            AreSame(articleRefsTask, articleRefsTask2);
            
            ReadRefsTask<Article> articleRefsTask3 = order1Task.ReadRefs(o => o.items.Select(a => a.article));
            AreSame(articleRefsTask, articleRefsTask3);
            AreEqual("Order['order-1'] .items[*].article", articleRefsTask.ToString());

            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask["article-1"]; });
            AreEqual("ReadRefsTask[] requires Sync(). Order['order-1'] .items[*].article", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask.Results; });
            AreEqual("ReadRefsTask.Results requires Sync(). Order['order-1'] .items[*].article", e.Message);

            // SubRefsTask<Producer> articleProducerTask = articleRefsTask.SubRef(a => a.producer);

            await store.Sync(); // -------- Sync --------
        
            AreEqual(2,                 articleRefsTask.Results.Count);
            AreEqual("Changed name",    articleRefsTask["article-1"].name);
            AreEqual("Smartphone",      articleRefsTask["article-2"].name);
        }
        
        private static async Task AssertEntityIdentity(PocStore store) {
            var orderTask = store.orders.Read("order-1");
            await store.Sync();
            
            var order = orderTask.Result;
            
            var articles    = store.articles;
            var customers   = store.customers;
            var orders      = store.orders;
            
            ReadTask<Order> order1Task = orders.Read("order-1");
            
            var article1Task            =  articles.Read("article-1");
            var article1TaskRedundant   =  articles.Read("article-1");
            AreSame(article1Task, article1TaskRedundant);
            
            var article2Task =  articles.Read("article-2");
            var customer1Task = customers.Read("customer-1");
            var unknownTask   = customers.Read("article-unknown");

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
            var orderTask = createStore.orders.Read("order-1");
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