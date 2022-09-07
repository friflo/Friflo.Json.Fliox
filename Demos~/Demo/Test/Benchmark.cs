using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Demo;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Remote;

namespace DemoTest {

    internal static class Benchmark
    {
        internal static async Task PubSubLatency()
        {
            var hub     = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
            var sender  = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            await PubSubLatencyCCU(sender, 1);
            await PubSubLatencyCCU(sender, 5);
            await PubSubLatencyCCU(sender, 10);
            await PubSubLatencyCCU(sender, 50);
            await PubSubLatencyCCU(sender, 100);
            await PubSubLatencyCCU(sender, 200);
            await PubSubLatencyCCU(sender, 300);
            await PubSubLatencyCCU(sender, 400);
            await PubSubLatencyCCU(sender, 500);
            await PubSubLatencyCCU(sender, 1000);
            await PubSubLatencyCCU(sender, 2000);
            await PubSubLatencyCCU(sender, 3000);
            await PubSubLatencyCCU(sender, 4000);
            await PubSubLatencyCCU(sender, 5000);
            await PubSubLatencyCCU(sender, 10000);
        }
        
        private static async Task PubSubLatencyCCU(FlioxClient sender, int ccu)
        {
            var start = DateTime.Now.Ticks;
            var connectTasks = new List<Task<BenchmarkContext>>();
            for (int n = 0; n < ccu; n++) {
                var client = ConnectClient();
                connectTasks.Add(client);
            }
            var contexts = await Task.WhenAll(connectTasks);
            
            var connected = DateTime.Now.Ticks;
            
            Console.WriteLine($"{ccu} clients connected. {(connected - start) / 10000} ms");
            
            // warmup
            for (int n = 0; n < 20; n++) { 
                sender.SendMessage("test", 0);
                await sender.SyncTasks();
                await Task.Delay(10);
            }
            
            for (int n = 0; n < 50; n++) { 
                sender.SendMessage("test", DateTime.Now.Ticks);   // message is published to all clients
                await sender.SyncTasks();
                await Task.Delay(50);
            }

            var diffs = contexts.Select(c => c.accumulatedLatency / (10000 * c.events)).ToArray();
            diffs = diffs.OrderBy(s => s).ToArray();
            if (diffs.Length > 10) {
                var diffSubset = new long[10];
                for (int n = 0; n < 10; n++) {
                    var index = (int)((diffs.Length - 1) * ((n + 1) / 10d));
                    diffSubset[n] = diffs[index];
                }
                diffs = diffSubset;
            }
            var diffStr = string.Join(' ', diffs);
            Console.WriteLine(diffStr);
            Console.WriteLine();

            var tasks = new List<Task>();
            foreach (var context in contexts) {
                context.client.UnsubscribeMessage("*", null);
                var task = context.client.SyncTasks();
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            foreach (var context in contexts) {
                context.client.Dispose();
                await context.hub.Close();
                context.hub.Dispose();
            }
        }
        
        private static async Task<BenchmarkContext> ConnectClient()
        {
            var hub                 = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
            var client              = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            
            var benchmarkContext    =  new BenchmarkContext { hub = hub, client = client };
            
            client.SubscribeMessage("*", (message, context) => {
                message.GetParam(out long start, out _);
                if (start == 0)
                    return;
                benchmarkContext.accumulatedLatency += DateTime.Now.Ticks - start;
                benchmarkContext.events++;
            });
            // client.SendMessage("xxx", 111);
            
            await client.SyncTasks();
            
            return benchmarkContext;
        }
    }
    
    internal class BenchmarkContext
    {
        internal    WebSocketClientHub          hub;
        internal    FlioxClient                 client;
        internal    long                        accumulatedLatency;
        internal    int                         events;   
    }
}
