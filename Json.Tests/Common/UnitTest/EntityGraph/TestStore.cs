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
            using (var database = new MemoryDatabase())
            using (var store = await TestRelationPoC.CreateStore(database)) {
                await WriteRead(store);
            }
        }
        
        [UnityTest] public IEnumerator FileCreateCoroutine() { yield return RunAsync.Await(FileCreate(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  FileCreateAsync() { await FileCreate(); }

        private async Task FileCreate() {
            using (var database = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var store = await TestRelationPoC.CreateStore(database)) {
                await WriteRead(store);
            }
        }
        
        [UnityTest] public IEnumerator FileEmptyCoroutine() { yield return RunAsync.Await(FileEmpty()); }
        [Test]      public async Task  FileEmptyAsync() { await FileEmpty(); }
        
        private async Task FileEmpty() {
            using (var database = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var store = new PocStore(database)) {
                await WriteRead(store);
            }
        }
        
        [UnityTest] public IEnumerator RemoteCreateCoroutine() { yield return RunAsync.Await(RemoteCreate()); }
        [Test]      public async Task  RemoteCreateAsync() { await RemoteCreate(); }
        
        private async Task RemoteCreate() {
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var hostDatabase = new RemoteHost(fileDatabase, "http://+:8080/")) {
                await RunRemoteHost(hostDatabase, async () => {
                    using (var clientDatabase = new RemoteClient("http://localhost:8080/"))
                    using (var clientStore = await TestRelationPoC.CreateStore(clientDatabase)) {
                        await WriteRead(clientStore);
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


        private async Task WriteRead(PocStore store) {
            // --- cache empty
            var order = store.orders.Read("order-1");
            await store.Sync();
            // var xxx = order.Result.customer.Entity;
            using (ObjectMapper mapper = new ObjectMapper(store.TypeStore)) {
                WriteRead(order.Result, mapper);
                await AssertStore(order.Result, store);
            }
        }
        
        private static void WriteRead(Order order, ObjectMapper mapper) {
            mapper.Pretty = true;
            
            AssertWriteRead(mapper, order);
            AssertWriteRead(mapper, order.customer);
            AssertWriteRead(mapper, order.items[0]);
            AssertWriteRead(mapper, order.items[1]);
            AssertWriteRead(mapper, order.items[0].article);
            AssertWriteRead(mapper, order.items[1].article);
        }

        private static bool lab = false;

        private static async Task AssertStore(Order order, PocStore store) {
            ReadTask<Order> order1 =    store.orders.Read("order-1");
            AreEqual("order-1", order1.ToString());
            var allArticles     = store.articles.QueryAll();
            var allArticles2    = store.articles.QueryByFilter(Operation.FilterTrue);
            var hasOrderCamera  = store.orders.Query(o => o.items.Any(i => i.name == "Camera"));
            var read1           = store.orders.Query(o => o.customer.id == "customer-1");
            var read2           = store.orders.Query(o => o.customer.Entity.lastName == "Smith");
            var read3           = store.orders.Query(o => o.items.Count(i => i.amount < 1) > 0);
            var read4           = store.orders.Query(o => o.items.Any(i => i.amount < 1));
            var read5           = store.orders.Query(o => o.items.All(i => i.amount < 1));
            var read6           = store.orders.Query(o => o.items.Any(i => i.article.Entity.name == "Smartphone"));

            
            ReadRefTask<Customer>     customer   = order1.ReadRefByPath<Customer>(".customer");
            ReadRefTask<Customer>     customer2  = order1.ReadRefByPath<Customer>(".customer");
            AreSame(customer, customer2);
            ReadRefTask<Customer>     customer3  = order1.ReadRef(o => o.customer);
            AreSame(customer, customer3);
            AreEqual("Order['order-1'] .customer", customer.ToString());
            
            Exception e;
            e = Throws<PeerNotSyncedException>(() => { var _ = customer.Id; });
            AreEqual("ReadRefTask.Id requires Sync(). Order['order-1'] .customer", e.Message);
            e = Throws<PeerNotSyncedException>(() => { var _ = customer.Result; });
            AreEqual("ReadRefTask.Result requires Sync(). Order['order-1'] .customer", e.Message);
            
            e = Throws<PeerNotSyncedException>(() => { var _ = hasOrderCamera.Result; });
            AreEqual("QueryTask.Result requires Sync(). Entity: Order filter: .items.Any(i => i.name == 'Camera')", e.Message);
            e = Throws<PeerNotSyncedException>(() => { var _ = hasOrderCamera[0]; });
            AreEqual("QueryTask[] requires Sync(). Entity: Order filter: .items.Any(i => i.name == 'Camera')", e.Message);

            // lab - test ReadRef expressions
            if (lab) {
                ReadRefsTask<Article> articles2 =   order1.ReadRefsOfType<Article>();
                ReadRefsTask<Entity> allDeps =      order1.ReadAllRefs();
            }

            await store.Sync();

            AreEqual(4,             allArticles.Result.Count);
            AreEqual(1,             hasOrderCamera.Result.Count);
            AreEqual("order-1",     hasOrderCamera[0].id);

            AreEqual("customer-1",  customer.Id);
            AreEqual("Smith",       customer.Result.lastName);

            // schedule ReadRefs on an already synced Read operation
            e = Throws<InvalidOperationException>(() => { order1.ReadRefByPath<Article>("customer"); });
            AreEqual("Used ReadTask is already synced. ReadTask<Order>, id: order-1", e.Message);
            e = Throws<InvalidOperationException>(() => { order1.ReadRefsByPath<Article>("items[*].article"); });
            AreEqual("Used ReadTask is already synced. ReadTask<Order>, id: order-1", e.Message);

            order1 =    store.orders.Read("order-1");
            ReadRefsTask<Article>    articleDeps    = order1.ReadRefsByPath<Article>(".items[*].article");
            ReadRefsTask<Article>    articleDeps2   = order1.ReadRefsByPath<Article>(".items[*].article");
            AreSame(articleDeps, articleDeps2);
            ReadRefsTask<Article>    articleDeps3   = order1.ReadRefs(o => o.items.Select(a => a.article));
            AreSame(articleDeps, articleDeps3);
            AreEqual("Order['order-1'] .items[*].article", articleDeps.ToString());
            
            e = Throws<PeerNotSyncedException>(() => { var _ = articleDeps[0]; });
            AreEqual("ReadRefsTask[] requires Sync(). Order['order-1'] .items[*].article", e.Message);
            e = Throws<PeerNotSyncedException>(() => { var _ = articleDeps.Results; });
            AreEqual("ReadRefsTask.Results requires Sync(). Order['order-1'] .items[*].article", e.Message);

            await store.Sync();
            AreEqual("article-1",       articleDeps[0].Id);
            AreEqual("Changed name",    articleDeps[0].Result.name);
            AreEqual("article-2",       articleDeps[1].Id);
            AreEqual("Smartphone",      articleDeps[1].Result.name);
            AreEqual("article-1",       articleDeps[2].Id);
            AreEqual("Changed name",    articleDeps[2].Result.name);
            
            var article1            =  store.articles.Read("article-1");
            var article1Redundant   =  store.articles.Read("article-1");
            AreSame(article1, article1Redundant);
            
            var article2 =  store.articles.Read("article-2");
            var customer1 = store.customers.Read("customer-1");
            var unknown   = store.customers.Read("article-unknown");

            await store.Sync();
            
            // AreEqual(1, store.customers.Count);
            // AreEqual(2, store.articles.Count);
            // AreEqual(1, store.orders.Count);

            AreSame(order1.   Result,   order);
            AreSame(customer1.Result,   order.customer.Entity);
            AreSame(article1. Result,   order.items[0].article.Entity);
            AreSame(article2. Result,   order.items[1].article.Entity);
            IsNull(unknown.Result);
        }

        private static void AssertWriteRead<T>(ObjectMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            // IsTrue(entity.Equals(result)); // references are equal
        }
    }
}