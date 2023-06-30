using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;

namespace TodoTest;

internal static class Trial
{
    // custom entry point to run test snippets with: dotnet run
    internal static async Task Main(string[] args)
    {
        await QueryAll(args);
        await SubscribeChangesAndMessages();
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
            case "http":    return new HttpClientHub("main_db", "http://localhost:8010/fliox/");
            case "ws":      return new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
            case "file":    return new FlioxHub(new FileDatabase("main_db", "./DB/main_db"));
            case "memory":  return new FlioxHub(new MemoryDatabase("main_db"));
        }
        throw new InvalidOperationException($"unknown option: '{option}' use: [http, ws, file, memory]");
    }
}
