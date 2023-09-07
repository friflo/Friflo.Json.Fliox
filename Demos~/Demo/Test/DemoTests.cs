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

namespace DemoTest;

public static class DemoTests
{
    private static readonly string          DbPath = GetBasePath() + "Demo/Test/DB/main_db";
    private static readonly DatabaseSchema  Schema = DatabaseSchema.Create<DemoClient>();

    private static EntityDatabase CreateDatabase(string dbName)
    {
        return new MemoryDatabase(dbName, Schema).AddCommands(new DemoCommands());
    }
    
    /// <summary>Seed temporary test database to avoid side effects by DB mutations</summary>
    private static void Seed(EntityDatabase database)
    {
        var seedDB      = new FileDatabase("source_db", DbPath);
        database.SeedDatabase(seedDB).Wait();
    }
    
    private static string GetBasePath()
    {
        string baseDir = Directory.GetCurrentDirectory() + "/../../../../../";
        return Path.GetFullPath(baseDir);
    }

    [Test]
    public static async Task QueryOrderRelations()
    {
        var database    = CreateDatabase("main_db");
        Seed(database);
        var hub         = new FlioxHub(database);
        var client      = new DemoClient(hub);
        var orders      = client.Orders.QueryAll();
        var articles    = orders.ReadRelations(client.Articles, o => o.items.Select(a => a.article));
        var producers   = articles.ReadRelations(client.Producers, o => o.producer);
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
        var database    = CreateDatabase("test");
        var hub         = new FlioxHub(database);
        var client      = new DemoClient(hub);
        client.Articles.Create (new Article { id = 111, name = "Article-1" });
        client.Employees.Create(new Employee { id = 222, firstName = "James", lastName = "Bond"});
        await client.SyncTasks();
        
        // create a second client to verify mutations
        var client2     = new DemoClient(hub);
        var articles    = client2.Articles.QueryAll();
        var employees   = client2.Employees.QueryAll();
        await client2.SyncTasks();
        
        var articleIds = articles.Result.Select(o => o.id);
        AreEquivalent(new long [] { 111 }, articleIds);
        
        var employeeIds = employees.Result.Select(o => o.id);
        AreEquivalent(new long [] { 222 }, employeeIds);
    }
    
    [Test]
    public static async Task DeleteEntities()
    {
        var database    = CreateDatabase("test");
        Seed(database);
        var hub         = new FlioxHub(database);
        var client      = new DemoClient(hub);
        var articles    = client.Articles.QueryAll();
        var customers   = client.Customers.QueryAll();
        await client.SyncTasks();
        
        // assert containers are not already empty
        AreEqual(4, articles.Result.Count);
        AreEqual(1, customers.Result.Count);

        // delete some entities            
        client.Articles.DeleteAll();
        client.Customers.Delete(12);
        await client.SyncTasks();
        
        // create a second client to verify mutations
        var client2     = new DemoClient(hub);
        var articles2   = client2.Articles.QueryAll();
        var customers2  = client2.Customers.QueryAll();
        await client2.SyncTasks();
        
        AreEqual(0, articles2.Result.Count);
        AreEqual(0, customers2.Result.Count);
    }
    
    /// <summary> demonstrate testing a custom database command: <see cref="DemoClient.FakeRecords"/> </summary>
    [Test]
    public static async Task CreateFakeRecords()
    {
        var database    = CreateDatabase("test");
        var hub         = new FlioxHub(database);
        var client      = new DemoClient(hub);
        var fake        = new Fake { articles = 1, customers = 2, employees = 3, orders = 4, producers = 5};
        var records     = client.FakeRecords(fake);
        await client.SyncTasks();
        
        IsTrue(records.Success);
        
        // create a second client to verify mutations
        var client2         = new DemoClient(hub);
        var articleCount    = client2.Articles.CountAll();
        var customerCount   = client2.Customers.CountAll();
        var employeeCount   = client2.Employees.CountAll();
        var orderCount      = client2.Orders.CountAll();
        var producerCount   = client2.Producers.CountAll();
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
        var database        = CreateDatabase("test");
        var hub             = new FlioxHub(database);
        hub.UsePubSub(EventDispatching.Send);    // dispatch events directly to simplify test
        
        // setup subscriber client
        var subClient       = new DemoClient(hub) { UserId = "admin", Token = "admin", ClientId = "sub-1" };
        var createdArticles = new List<long[]>();
        subClient.Articles.SubscribeChanges(Change.All, (changes, context) => {
            var created = changes.Creates.Select(create => create.key).ToArray();
            createdArticles.Add(created);
        });
        await subClient.SyncTasks();
        
        // perform change with different client
        var client    = new DemoClient(hub) { UserId = "admin", Token = "admin" };
        client.Articles.Create(new Article{id = 10, name = "Article-10"});
        await client.SyncTasks();
        
        AreEqual(1,                 createdArticles.Count);
        AreEqual(new long[] { 10 }, createdArticles[0]);
        
        // unsubscribe to free subscription resources on Hub
        subClient.Articles.SubscribeChanges(Change.None, (changes, context) => { });
        await subClient.SyncTasks();
        AreEqual(1, hub.EventDispatcher.SubscribedClientsCount);
    }
    
    [Test]
    public static async Task SubscribeMessage()
    {
        var database        = CreateDatabase("test");
        var hub             = new FlioxHub(database);
        hub.UsePubSub(EventDispatching.Send);    // dispatch events directly to simplify test
        
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