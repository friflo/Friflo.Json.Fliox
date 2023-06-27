using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using NUnit.Framework;
using Todo;
using static NUnit.Framework.Assert;

namespace TodoTest;

public static class TodoTests
{
    private static readonly string          DbPath = GetBasePath() + "Todo/Test/DB/main_db";
    private static readonly DatabaseSchema  Schema = DatabaseSchema.Create<TodoClient>();

    /// <summary>create a <see cref="MemoryDatabase"/> clone for every client to avoid side effects by DB mutations</summary>
    private static FlioxHub CreateTodoHub()
    {
        var cloneDB = CreateMemoryDatabaseClone("main_db", DbPath);
        return new FlioxHub(cloneDB);
    }

    private static string GetBasePath()
    {
        string baseDir = Directory.GetCurrentDirectory() + "/../../../../../";
        return Path.GetFullPath(baseDir);
    }

    private static MemoryDatabase CreateMemoryDatabaseClone(string dbName, string srcDatabasePath)
    {
        var referenceDB = new FileDatabase("source_db", srcDatabasePath);
        var cloneDB     = new MemoryDatabase(dbName, Schema);
        cloneDB.SeedDatabase(referenceDB).Wait();
        return cloneDB;
    }
    
    [Test]
    public static async Task QueryAllJobs()
    {
        var hub         = CreateTodoHub();
        var client      = new TodoClient(hub);
        var jobs        = client.jobs.QueryAll();
        await client.SyncTasks();
        
        AreEqual(2, jobs.Result.Count);
        AreEqual(2, client.jobs.Local.Count);
    }
    
    [Test]
    public static async Task QueryFilterJobs()
    {
        var hub         = CreateTodoHub();
        var client      = new TodoClient(hub);
        var jobs        = client.jobs.Query(o => o.completed == true);
        await client.SyncTasks();
        
        AreEqual(1, jobs.Result.Count);
        AreEqual(1, client.jobs.Local.Count);
    }
    
    [Test]
    public static async Task QueryCursorJobs()
    {
        var hub         = CreateTodoHub();
        var client      = new TodoClient(hub);
        var query       = client.jobs.QueryAll();
        query.maxCount  = 1; // query with cursor
        var count       = 0;
        while (true) {
            await client.SyncTasks();
            
            count += query.Result.Count;
            if (query.IsFinished)
                break;
            query = query.QueryNext();
        }
        AreEqual(2, count);
    }
    
    [Test]
    public static async Task CountAllJobs()
    {
        var hub         = CreateTodoHub();
        var client      = new TodoClient(hub);
        var jobs        = client.jobs.CountAll();
        await client.SyncTasks();
        
        AreEqual(2, jobs.Result);
    }
    
    [Test]
    public static async Task CountFilterJobs()
    {
        var hub         = CreateTodoHub();
        var client      = new TodoClient(hub);
        var jobs        = client.jobs.Count(o => o.completed == true);
        await client.SyncTasks();
        
        AreEqual(1, jobs.Result);
    }
    
    [Test]
    public static async Task FindJob()
    {
        var hub         = CreateTodoHub();
        var client      = new TodoClient(hub);
        var readJob     = client.jobs.Read().Find(1);
        await client.SyncTasks();
        
        AreEqual("buy milk", readJob.Result.title);
        AreEqual(1, client.jobs.Local.Count);
    }
    
    [Test]
    public static async Task FindRangeJob()
    {
        var hub         = CreateTodoHub();
        var client      = new TodoClient(hub);
        var readJob     = client.jobs.Read().FindRange(new long [] { 1, 2, 3 });
        await client.SyncTasks();
        
        AreEqual(3, readJob.Result.Count);
        NotNull(readJob.Result[1]);
        NotNull(readJob.Result[2]);
        IsNull (readJob.Result[3]);
        AreEqual(3, client.jobs.Local.Count);
    }
}