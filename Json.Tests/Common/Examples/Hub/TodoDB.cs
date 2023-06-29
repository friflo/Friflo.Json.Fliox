#if !UNITY_5_3_OR_NEWER

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.AspNetCore;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.SQLite;
using Microsoft.AspNetCore.Builder;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.Examples.Hub
{
public class TodoDB : FlioxClient
{
    public readonly EntitySet <long, Job>     jobs;

    public TodoDB(FlioxHub hub, string dbName = null) : base(hub, dbName) { }
}

public class Job
{
    public  long        id;
    public  string      name;
    public  bool        completed;
}

public static class TestTodoDB
{
[Test]
public static async Task DirectDatabaseAccess()
{
    var schema      = DatabaseSchema.Create<TodoDB>();
    var database    = new SQLiteDatabase("todo_db", "Data Source=todo.sqlite3", schema);
    var hub         = new FlioxHub(database);
    
    var client      = new TodoDB(hub);
    client.jobs.UpsertRange(new[] {
        new Job { id = 1, name = "Buy milk", completed = true },
        new Job { id = 2, name = "Buy cheese", completed = false }
    });
    var jobs = client.jobs.Query(job => job.completed == true);
    await client.SyncTasks(); // execute UpsertRange & Query task
    
    foreach (var job in jobs.Result) {
        Console.WriteLine($"{job.id}: {job.name}");
    }
    // output:  1: Buy milk
}

// [Test]
public static void RunServer()
{
    var schema      = DatabaseSchema.Create<TodoDB>();
    var database    = new SQLiteDatabase("todo_db", "Data Source=todo.sqlite3", schema);
    var hub         = new FlioxHub(database);
    
    hub.UseClusterDB(); // required by HubExplorer
    hub.UsePubSub();
    var httpHost    = new HttpHost(hub, "/fliox/");
    httpHost.UseStaticFiles(HubExplorer.Path); // optional: Hub Explorer Web UI
    var app         = WebApplication.Create();
    app.UseWebSockets();
    app.MapHost("/fliox/{*path}", httpHost);
    app.Run();      // "http://localhost:5000/fliox/"
}

// [Test]
public static async Task AccessRemoteHub()
{
    var hub     = new WebSocketClientHub("todo_db", "http://localhost:5000/fliox/");
    var client  = new TodoDB(hub);
    var jobs    = client.jobs.Query(job => job.completed == true);
    client.jobs.SubscribeChanges(Change.All, (changes, context) => {
        Console.WriteLine(changes);
    });
    await client.SyncTasks(); // execute Query & SubscribeChanges task
    
    foreach (var job in jobs.Result) {
        Console.WriteLine($"{job.id}: {job.name}");
    }
    // output:  1: Buy milk
}
}
}

#endif