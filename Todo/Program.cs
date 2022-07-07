using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;

namespace Todo
{
    internal  static class  Program
    {
        public static async Task Main(string[] args) {
            var hub     = CreateHub(args);
            var client  = new TodoClient(hub);
            var jobs    = client.jobs.QueryAll();
            await client.SyncTasks();
            
            foreach (var job in jobs.Result) {
                Console.WriteLine($"id: {job.id}, title: {job.title}, completed: {job.completed}");
            }
        }
        
        private static FlioxHub CreateHub(string[] args) {
            var option = args.FirstOrDefault();
            switch (option) {
                case null:
                case "http":
                    return new HttpClientHub("main_db", "http://localhost:8010/fliox/");
                case "ws":
                    var wsHub = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
                    wsHub.Connect().Wait();
                    return wsHub;
                case "file":
                    var db = new FileDatabase("main_db", "../TodoHub/DB~/main_db");
                    return new FlioxHub(db);
            }
            throw new InvalidOperationException($"unknown option: '{option}' use: [http, ws, file]");
        }
    }
}
