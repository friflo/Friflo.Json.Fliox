using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.EntityGraph;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.Mapper;
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
        [UnityTest] public IEnumerator  CollectAwaitCoroutine() { yield return RunAsync.Await(CollectAwait(), i => Log.Info("--- " + i)); }
        [Test]      public async Task   CollectAwaitAsync() { await CollectAwait(); }
        
        private async Task CollectAwait() {
            List<Task> tasks = new List<Task>();
            for (int n = 0; n < 1000; n++) {
                Task task = Task.Delay(1);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }

        [UnityTest] public IEnumerator  ChainAwaitCoroutine() { yield return RunAsync.Await(ChainAwait(), i => Log.Info("--- " + i)); }
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
        
        [UnityTest] public IEnumerator FileCreateCoroutine() { yield return RunAsync.Await(FileCreate(), i => Log.Info("--- " + i)); }
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
                Log.Info("1. RemoteHost finished");
            });
            
            await run();
            
            await remoteHost.Stop();
            await hostTask;
            Log.Info("2. awaited hostTask");
        } 


        private async Task WriteRead(PocStore store) {
            // --- cache empty
            var order = store.orders.Read("order-1");
            await store.Sync();
            // var xxx = order.Result.customer.Entity;

            WriteRead(order.Result, store);
            await AssertStore(order.Result, store);
        }
        
        private static void WriteRead(Order order, EntityStore store) {
            var m = store.intern.jsonMapper;
            m.Pretty = true;
            
            AssertWriteRead(m, order);
            AssertWriteRead(m, order.customer);
            AssertWriteRead(m, order.items[0]);
            AssertWriteRead(m, order.items[1]);
            AssertWriteRead(m, order.items[0].article);
            AssertWriteRead(m, order.items[1].article);
        }

        private static bool lab = false;

        private static async Task AssertStore(Order order, PocStore store) {
            Read<Order> order1 =    store.orders.Read("order-1");
            Dependency<Customer>     customer   = order1.DependencyByPath<Customer>(".customer");
            Dependency<Customer>     customer2  = order1.DependencyByPath<Customer>(".customer");
            AreSame(customer, customer2);
            Dependency<Customer>     customer3  = order1.Dependency(o => o.customer);
            AreSame(customer, customer3);
            
            var e = Throws<PeerNotSyncedException>(() => { var _ = customer.Id; });
            AreEqual("Dependency not synced. Dependency<Customer>", e.Message);
            e = Throws<PeerNotSyncedException>(() => { var _ = customer.Result; });
            AreEqual("Dependency not synced. Dependency<Customer>", e.Message);
            
            // lab - test dependency expressions
            if (lab) {
                Dependencies<Article> articles2 =   order1.DependenciesOfType<Article>();
                Dependencies<Entity> allDeps =      order1.AllDependencies();
            }

            await store.Sync();
            AreEqual("customer-1",  customer.Id);
            AreEqual("Smith",       customer.Result.lastName);
            
            order1 =    store.orders.Read("order-1"); // todo assert reusing order1 or implicit read the parent entity
            Dependencies<Article>    articleDeps    = order1.DependenciesByPath<Article>(".items[*].article");
            Dependencies<Article>    articleDeps2   = order1.DependenciesByPath<Article>(".items[*].article");
            AreSame(articleDeps, articleDeps2);
            Dependencies<Article>    articleDeps3   = order1.Dependencies(o => o.items.Select(a => a.article));
            AreSame(articleDeps, articleDeps3);
            
            e = Throws<PeerNotSyncedException>(() => { var _ = articleDeps[0].Id; });
            AreEqual("Dependencies not synced. Dependencies<Article>", e.Message);
            e = Throws<PeerNotSyncedException>(() => { var _ = articleDeps.Results; });
            AreEqual("Dependencies not synced. Dependencies<Article>", e.Message);

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

        private static void AssertWriteRead<T>(JsonMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            // IsTrue(entity.Equals(result)); // references are equal
        }
    }
}