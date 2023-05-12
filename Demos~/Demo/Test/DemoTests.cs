using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Demo;
using DemoHub;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using NUnit.Framework;
using static NUnit.Framework.CollectionAssert;
using static NUnit.Framework.Assert;

namespace DemoTest {

    public static class DemoTests
    {
        private static readonly string DbPath = GetBasePath() + "Demo/Test/DB/main_db";

        /// <summary>create a <see cref="MemoryDatabase"/> clone for every client to avoid side effects by DB mutations</summary>
        private static FlioxHub CreateDemoHub()
        {
            var cloneDB = CreateMemoryDatabaseClone("main_db", DbPath, new DemoService());
            return new FlioxHub(cloneDB);
        }
        
        private static string GetBasePath()
        {
            string baseDir = Directory.GetCurrentDirectory() + "/../../../../../";
            return Path.GetFullPath(baseDir);
        }
    
        private static MemoryDatabase CreateMemoryDatabaseClone(string dbName, string srcDatabasePath, DatabaseService service = null)
        {
            var referenceDB = new FileDatabase("source_db", srcDatabasePath);
            var cloneDB     = new MemoryDatabase(dbName, service);
            cloneDB.SeedDatabase(referenceDB).Wait();
            return cloneDB;
        }
        
        [Test]
        public static async Task QueryOrderRelations()
        {
            var hub         = CreateDemoHub();
            var client      = new DemoClient(hub);
            var orders      = client.orders.QueryAll();
            var articles    = orders.ReadRelations(client.articles, o => o.items.Select(a => a.article));
            var producers   = articles.ReadRelations(client.producers, o => o.producer);
            await client.SyncTasks();
            
            var orderIds = orders.Result.Select(o => o.id);
            AreEquivalent(new long [] { 14, 24 }, orderIds);
            
            var articleIds = articles.Result.Select(o => o.id);
            AreEquivalent(new long [] { 11, 21 }, articleIds);
            
            var producerIds = producers.Result.Select(o => o.id);
            AreEquivalent(new long [] { 25 }, producerIds);
        }
        
        [Test]
        public static async Task CreateEntities()
        {
            var database    = new MemoryDatabase("test", new DemoService());
            var hub         = new FlioxHub(database);
            var client      = new DemoClient(hub);
            client.articles.Create (new Article { id = 111, name = "Article-1" });
            client.employees.Create(new Employee { id = 222, firstName = "James", lastName = "Bond"});
            await client.SyncTasks();
            
            // create a second client to verify mutations
            var client2     = new DemoClient(hub);
            var articles    = client2.articles.QueryAll();
            var employees   = client2.employees.QueryAll();
            await client2.SyncTasks();
            
            var articleIds = articles.Result.Select(o => o.id);
            AreEquivalent(new long [] { 111 }, articleIds);
            
            var employeeIds = employees.Result.Select(o => o.id);
            AreEquivalent(new long [] { 222 }, employeeIds);
        }
        
        [Test]
        public static async Task DeleteEntities()
        {
            var hub         = CreateDemoHub();
            var client      = new DemoClient(hub);
            
            var articles    = client.articles.QueryAll();
            var customers   = client.customers.QueryAll();
            await client.SyncTasks();
            
            // assert containers are not already empty
            AreEqual(4, articles.Result.Count);
            AreEqual(1, customers.Result.Count);

            // delete some entities            
            client.articles.DeleteAll();
            client.customers.Delete(12);
            await client.SyncTasks();
            
            // create a second client to verify mutations
            var client2     = new DemoClient(hub);
            var articles2   = client2.articles.QueryAll();
            var customers2  = client2.customers.QueryAll();
            await client2.SyncTasks();
            
            AreEqual(0, articles2.Result.Count);
            AreEqual(0, customers2.Result.Count);
        }
        
        /// <summary> demonstrate testing a custom database command: <see cref="DemoClient.FakeRecords"/> </summary>
        [Test]
        public static async Task CreateFakeRecords()
        {
            var database    = new MemoryDatabase("test", new DemoService());
            var hub         = new FlioxHub(database);
            var client      = new DemoClient(hub);
            var fake        = new Fake { articles = 1, customers = 2, employees = 3, orders = 4, producers = 5};
            var records     = client.FakeRecords(fake);
            await client.SyncTasks();
            
            IsTrue(records.Success);
            
            // create a second client to verify mutations
            var client2         = new DemoClient(hub);
            var articleCount    = client2.articles.CountAll();
            var customerCount   = client2.customers.CountAll();
            var employeeCount   = client2.employees.CountAll();
            var orderCount      = client2.orders.CountAll();
            var producerCount   = client2.producers.CountAll();
            await client2.SyncTasks();
            
            AreEqual(1, articleCount.Result);
            AreEqual(2, customerCount.Result);
            AreEqual(3, employeeCount.Result);
            AreEqual(4, orderCount.Result);
            AreEqual(5, producerCount.Result);
        }
        
        [Test]
        public static async Task SubscribeChanges()
        {
            var database        = new MemoryDatabase("test", new DemoService());
            var hub             = new FlioxHub(database);
            hub.EventDispatcher = new EventDispatcher(EventDispatching.Send); // dispatch events directly to simplify test
            
            // setup subscriber client
            var subClient       = new DemoClient(hub) { UserId = "admin", Token = "admin", ClientId = "sub-1" };
            var createdArticles = new List<long[]>();
            subClient.articles.SubscribeChanges(Change.All, (changes, context) => {
                var created = changes.Creates.Select(create => create.key).ToArray();
                createdArticles.Add(created);
            });
            await subClient.SyncTasks();
            
            // perform change with different client
            var client    = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            client.articles.Create(new Article{id = 10, name = "Article-10"});
            await client.SyncTasks();
            
            AreEqual(1,                 createdArticles.Count);
            AreEqual(new long[] { 10 }, createdArticles[0]);
            
            // unsubscribe to free subscription resources on Hub
            subClient.articles.SubscribeChanges(Change.None, (changes, context) => { });
            await subClient.SyncTasks();
            AreEqual(1, hub.EventDispatcher.SubscribedClientsCount);
        }
        
        [Test]
        public static async Task SubscribeMessage()
        {
            var database        = new MemoryDatabase("test", new DemoService());
            var hub             = new FlioxHub(database);
            hub.EventDispatcher = new EventDispatcher(EventDispatching.Send); // dispatch events directly to simplify test
            
            // setup subscriber client
            var subClient       = new DemoClient(hub) { UserId = "admin", Token = "admin", ClientId = "sub-2" };
            var addOperands     = new List<Operands>();
            subClient.SubscribeMessage<Operands>("demo.Add", (message, context) => {
                message.GetParam(out var operands, out _);
                addOperands.Add(operands);
            });
            await subClient.SyncTasks();
            
            // send command
            var client    = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            client.Add(new Operands { left = 11, right = 22 });
            // when setting an event target (client, user or group) only those targets receive an event 
            client.Add(new Operands { left = 33, right = 44 }).EventTargetGroup("some-group");
            await client.SyncTasks();
            
            AreEqual(1,     addOperands.Count);
            AreEqual(11,    addOperands[0].left);
            AreEqual(22,    addOperands[0].right);
            
            // unsubscribe to free subscription resources on Hub
            subClient.UnsubscribeMessage("demo.Add", null);
            await subClient.SyncTasks();
            AreEqual(1, hub.EventDispatcher.SubscribedClientsCount);
        }
    }
}