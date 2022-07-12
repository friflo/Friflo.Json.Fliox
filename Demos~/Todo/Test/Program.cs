using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Todo;

namespace TodoTest {

    internal static class Program
    {
        internal static async Task Main(string[] args) {
            var option  = args.FirstOrDefault() ?? "http";
            var hub     = CreateHub(option);
            var client  = new TodoClient(hub);
            var jobs    = client.jobs.QueryAll();
            await client.SyncTasks();
                
            foreach (var job in jobs.Result) {
                Console.WriteLine($"id: {job.id}, title: {job.title}, completed: {job.completed}");
            }
        }
            
        private static FlioxHub CreateHub(string option) {            
            switch (option) {
                case "http":
                    return new HttpClientHub("main_db", "http://localhost:8010/fliox/");
                case "ws":
                    var wsHub = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
                    wsHub.Connect().Wait();
                    return wsHub;
                case "file":
                    var db = new FileDatabase("main_db", "../TodoHub/DB/main_db");
                    return new FlioxHub(db);
            }
            throw new InvalidOperationException($"unknown option: '{option}' use: [http, ws, file]");
        }
    }
}
