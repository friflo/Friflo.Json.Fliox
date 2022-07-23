using System;
using System.Linq;
using System.Threading.Tasks;
using Demo;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;

namespace DemoTest {

    internal static class Trial
    {
        // custom entry point called by: dotnet run
        internal static async Task Main2(string[] args)
        {
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
        
        internal static async Task Main(string[] args)
        {
            var option      = args.FirstOrDefault() ?? "ws";
            var hub         = CreateHub(option);
            var client      = new DemoClient(hub) { UserId = "admin", Token = "admin", ClientId="TestSub" };
            client.articles.SubscribeChanges(ChangeFlags.All, (changes, context) => {
                foreach (var item in changes.Upserts)
                {
                    Console.WriteLine($"article: {item.name}");                    
                }
            });
            await client.SyncTasks();
            Console.WriteLine("wait for events ...");

            await Task.Run ( ( ) => Console.ReadKey ( true ) );
            Console.ReadKey();
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
