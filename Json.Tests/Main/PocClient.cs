using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;

namespace Friflo.Json.Tests.Main
{
    public static class PocClient
    {
        public static async Task ListenEvents() {
            var hub         = CreateHub("ws");
            var client      = new PocStore(hub) { UserId = "admin", Token = "admin", ClientId="TestClient" };
            client.SubscriptionEventHandler = context => {
                Task.Run(() => client.SyncTasks()); // acknowledge received event to the Hub
            };
            client.articles.SubscribeChanges(Change.All, (changes, context) => {
                foreach (var entity in changes.Upserts) {
                    if (context.EventSeq % 100 == 0) {
                        Console.WriteLine($"EventSeq: {context.EventSeq} - upsert article: {entity.id}, name: {entity.name}");
                    }
                }
                foreach (var key in changes.Deletes) {
                    Console.WriteLine($"EventSeq: {context.EventSeq} - delete article: {key}");
                }
            });
            await client.SyncTasks();
            
            Console.WriteLine("\n wait for events ... (exit with: CTRL + C) note: generate events by clicking 'Save' on an article in the Hub Explorer\n");
            await Task.Delay(3_600_000); // wait 1 hour
        }
        
        private static FlioxHub CreateHub(string option)
        {
            switch (option) {
                case "http":    return new HttpClientHub("main_db", "http://localhost:8010/fliox/");
                case "ws":  // todo simplify
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