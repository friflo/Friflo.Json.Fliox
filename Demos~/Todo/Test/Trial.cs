using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.SQLite;

namespace TodoTest;

internal static class Trial
{
    // custom entry point to run test snippets with: dotnet run
    internal static async Task Main(string[] args)
    {
        // await DirectDatabaseExample();
        // await RemoteDatabaseExample();
        await QueryAll(args);
        await SubscribeChangesAndMessages();
    }
    
    // Example used at: https://github.com/friflo/Friflo.Json.Fliox#direct-database-access
    private static async Task DirectDatabaseExample() {
        var schema      = DatabaseSchema.Create<TodoClient>();
        var database    = new SQLiteDatabase("todo_db", "Data Source=todo.sqlite3", schema);
        var hub         = new FlioxHub(database);

        var client      = new TodoClient(hub);
        client.jobs.UpsertRange(new[] {
            new Job { id = 1, title = "Buy milk", completed = true },
            new Job { id = 2, title = "Buy cheese", completed = false }
        });
        var jobs = client.jobs.Query(job => job.completed == true);
        await client.SyncTasks(); // execute UpsertRange & Query task
    
        foreach (var job in jobs.Result) {
            Console.WriteLine($"{job.id}: {job.title}");
        }
        // output:  1: Buy milk
    }
    
    // Example used at: https://github.com/friflo/Friflo.Json.Fliox#remote-database-access
    private static async Task RemoteDatabaseExample() {
        var hub     = new WebSocketClientHub("todo_db", "ws://localhost:5000/fliox/");
        var client  = new TodoClient(hub);
        var jobs    = client.jobs.Query(job => job.completed == true);
        client.jobs.SubscribeChanges(Change.All, (changes, context) => {
            Console.WriteLine(changes);
        });
        await client.SyncTasks(); // execute Query & SubscribeChanges task
    
        foreach (var job in jobs.Result) {
            Console.WriteLine($"{job.id}: {job.title}");
        }
        // output:  1: Buy milk
        Console.WriteLine("\n wait for events ... (exit with: CTRL + C)\n note: generate events by clicking 'Save' on a record in the Hub Explorer\n");
        await Task.Delay(3_600_000); // wait 1 hour
    }

    private static async Task  QueryAll(string[] args)
    {
        var option  = args.FirstOrDefault() ?? "http";
        var hub     = CreateHub(option);
        var client  = new TodoClient(hub);
        var jobs    = client.jobs.QueryAll();
        await client.SyncTasks();

        Console.WriteLine($"\n--- jobs:");
        foreach (var job in jobs.Result) {
            Console.WriteLine($"id: {job.id}, title: {job.title}, completed: {job.completed}");
        }
    }
    
    // after calling this method open: 'Hub Explorer > main_db > articles'
    // changing records in 'articles' trigger the subscription handler in this method.  
    private static async Task SubscribeChangesAndMessages()
    {
        var hub         = CreateHub("ws");
        var client      = new TodoClient(hub) { UserId = "admin", Token = "admin" };
        client.jobs.SubscribeChanges(Change.All, (changes, context) => {
            foreach (var upsert in changes.Upserts) {
                var job = upsert.entity;
                Console.WriteLine($"EventSeq: {context.EventSeq} - upsert job: {job.id}, name: {job.title}");
            }
            foreach (var key in changes.Deletes) {
                Console.WriteLine($"EventSeq: {context.EventSeq} - delete job: {key}");
            }
        });
        // subscribe all messages
        client.SubscribeMessage("*", (message, context) => {
            Console.WriteLine($"EventSeq: {context.EventSeq} - message: {message}");
        });
        client.SubscribeMessage<string>("TestMessage", (message, context) => {
            message.GetParam (out string param, out _);
            Console.WriteLine($"EventSeq: {context.EventSeq} - TestMessage ('{param}')");
        });
        await client.SyncTasks();
        
        Console.WriteLine("\n wait for events ... (exit with: CTRL + C)\n note: generate events by clicking 'Save' on a record in the Hub Explorer\n");
        await Task.Delay(3_600_000); // wait 1 hour
    }
        
    private static FlioxHub CreateHub(string option)
    {            
        switch (option) {
            case "http":    return new HttpClientHub("main_db", "http://localhost:5000/fliox/");
            case "ws":      return new WebSocketClientHub("main_db", "ws://localhost:5000/fliox/");
            case "file":    return new FlioxHub(new FileDatabase("main_db", "./DB/main_db"));
            case "memory":  return new FlioxHub(new MemoryDatabase("main_db"));
        }
        throw new InvalidOperationException($"unknown option: '{option}' use: [http, ws, file, memory]");
    }
}
