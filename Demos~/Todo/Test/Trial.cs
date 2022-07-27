using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Todo;

namespace TodoTest {

    internal static class Trial
    {
        // custom entry point to run test snippets with: dotnet run
        internal static async Task Main(string[] args)
        {
            await QueryAll(args);
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
            
        private static FlioxHub CreateHub(string option)
        {            
            switch (option) {
                case "http":    return new HttpClientHub("main_db", "http://localhost:8010/fliox/");
                case "ws":
                    var wsHub = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
                    wsHub.Connect().Wait();
                    return wsHub;
                case "file":    return new FlioxHub(new FileDatabase("main_db", "./DB/main_db"));
                case "memory":  return new FlioxHub(new MemoryDatabase("main_db"));
            }
            throw new InvalidOperationException($"unknown option: '{option}' use: [http, ws, file, memory]");
        }
    }
}
