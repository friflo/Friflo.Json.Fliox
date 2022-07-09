using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;

namespace Demo
{
    internal  static class  Program
    {
        public static async Task Main(string[] args) {
            var option      = args.FirstOrDefault() ?? "http";
            var hub         = CreateHub(option);
            var client      = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            var orders      = client.orders.QueryAll();
            var articles    = orders.ReadRelations(client.articles, o => o.items.Select(a => a.article));
            await client.SyncTasks();
            
            Console.WriteLine($"\n--- orders:");
            foreach (var order in orders.Result) {
                Console.WriteLine($"id: {order.id}, created: {order.created}");
            }
            Console.WriteLine($"\n--- articles");
            foreach (var article in articles.Result) {
                Console.WriteLine($"id: {article.id}, name: {article.name}");
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
                    var db = new FileDatabase("main_db", "../DemoHub/DB~/main_db");
                    return new FlioxHub(db);
            }
            throw new InvalidOperationException($"unknown option: '{option}' use: [http, ws, file]");
        }
    }
}
