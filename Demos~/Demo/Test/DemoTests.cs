using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Demo;
using DemoHub;
using Friflo.Json.Fliox.Hub.Host;
using NUnit.Framework;
using static NUnit.Framework.CollectionAssert;
using static NUnit.Framework.Assert;

namespace DemoTest {

    public static class DemoTests
    {
        private static readonly string DbPath = GetBasePath() + "Demo/Hub/DB/main_db";

        /// <summary>create a <see cref="MemoryDatabase"/> clone for every client to avoid side effects by DB mutations</summary>
        private static FlioxHub CreateDemoHub() {
            var cloneDB = CreateMemoryDatabaseClone("main_db", DbPath, new MessageHandler());
            return new FlioxHub(cloneDB);
        }
        
        private static string GetBasePath() {
            string baseDir = Directory.GetCurrentDirectory() + "/../../../../../";
            return Path.GetFullPath(baseDir);
        }
    
        private static MemoryDatabase CreateMemoryDatabaseClone(string dbName, string srcDatabasePath, TaskHandler taskHandler = null) {
            var referenceDB = new FileDatabase("source_db", srcDatabasePath);
            var cloneDB     = new MemoryDatabase(dbName, taskHandler);
            cloneDB.SeedDatabase(referenceDB).Wait();
            return cloneDB;
        }
        
        [Test]
        public static async Task QueryOrderRelations() {
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
        public static async Task CreateEntities() {
            var database    = new MemoryDatabase("test", new MessageHandler());
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
        public static async Task DeleteEntities() {
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
            var articles2    = client2.articles.QueryAll();
            var customers2   = client2.customers.QueryAll();
            await client2.SyncTasks();
            
            AreEqual(0, articles2.Result.Count);
            AreEqual(0, customers2.Result.Count);
        }
        
        /// <summary> demonstrate testing a custom database command: <see cref="DemoClient.FakeRecords"/> </summary>
        [Test]
        public static async Task CreateFakeRecords() {
            var database    = new MemoryDatabase("test", new MessageHandler());
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
    }
}