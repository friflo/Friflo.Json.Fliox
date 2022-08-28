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
            var lapEvents   = 0;
            var lapStart    = new DateTime();
            var hub         = CreateHub();
            var client      = new PocStore(hub) { UserId = "admin", Token = "admin", ClientId = clientId };
            client.SubscriptionEventHandler = context => {
                lapEvents++;  
            };
            client.articles.SubscribeChanges(Change.All, (changes, context) => {
                if (context.EventSeq <= 20 || context.EventSeq % 1000 == 0) {
                    Console.WriteLine($"EventSeq: {context.EventSeq} - Upserts: {changes.Upserts.Count} Deletes: {changes.Deletes.Count}");
                    if (context.EventSeq == 20) Console.WriteLine($"  from now: log only every 1000 event");
                }
            });
            client.SubscribeMessage("*", (name, handler) => {
                if (handler.EventSeq <= 20 || handler.EventSeq % 1000 == 0) {
                    Console.WriteLine($"EventSeq: {handler.EventSeq} - Messages: {handler.Messages.Count}");
                    if (handler.EventSeq == 20) Console.WriteLine($"  from now: log only every 1000 event");
                }
            });
            client.SubscribeMessage<DateTime>("StartTime", (message, context) => {
                message.GetParam(out lapStart, out _);
                lapEvents = 0;
            });
            client.SubscribeMessage<DateTime>("StopTime", (message, context) => {
                var dif = DateTime.Now - lapStart;
                var throughput = lapEvents * 1000d / dif.TotalMilliseconds;
                Console.WriteLine($"--- events: {lapEvents}, throughput: {throughput:0} events/sec");
            });
            await client.SyncTasks();
            
            Console.WriteLine("\n wait for events ... (exit with: CTRL + C) note: generate events by clicking 'Save' on an article in the Hub Explorer\n");
            await Task.Delay(3_600_000); // wait 1 hour
        }
        
        private static FlioxHub CreateHub() {
            var wsHub = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
            // wsHub.Connect().Wait();
            return wsHub;
        }
    }
}