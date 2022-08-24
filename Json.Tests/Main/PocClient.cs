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
        public static async Task ListenEvents(string clientId) {
            var hub         = CreateHub("ws");
            var client      = new PocStore(hub) { UserId = "admin", Token = "admin", ClientId = clientId };
            client.articles.SubscribeChanges(Change.All, (changes, context) => {
                if (context.EventSeq <= 20 || context.EventSeq % 1000 == 0) {
                    Console.WriteLine($"EventSeq: {context.EventSeq} - Upserts: {changes.Upserts.Count} Deletes: {changes.Deletes.Count}");
                    if (context.EventSeq == 20) Console.WriteLine($"  from now: log only every 1000 event");
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