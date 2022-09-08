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
            
            var tickRate = 50;
            Console.WriteLine($"tickRate: {tickRate}");
            Console.WriteLine("      latency [ms] percentiles [%]    50    95    96    97    98    99   100");
            await PubSubLatencyCCU(sender, tickRate, 2);
            await PubSubLatencyCCU(sender, tickRate, 2);
            await PubSubLatencyCCU(sender, tickRate, 5);
            await PubSubLatencyCCU(sender, tickRate, 10);
            await PubSubLatencyCCU(sender, tickRate, 50);
            Console.WriteLine();
            await PubSubLatencyCCU(sender, tickRate, 100);
            await PubSubLatencyCCU(sender, tickRate, 200);
            await PubSubLatencyCCU(sender, tickRate, 300);
            await PubSubLatencyCCU(sender, tickRate, 400);
            await PubSubLatencyCCU(sender, tickRate, 500);
            Console.WriteLine();
            await PubSubLatencyCCU(sender, tickRate, 1000);
            await PubSubLatencyCCU(sender, tickRate, 2000);
            await PubSubLatencyCCU(sender, tickRate, 3000);
            await PubSubLatencyCCU(sender, tickRate, 4000);
            // await PubSubLatencyCCU(sender, 5000);
            //await PubSubLatencyCCU(sender, 10000);
        }
        
        private static async Task PubSubLatencyCCU(FlioxClient sender, int tickRate, int ccu)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            var start = DateTime.Now.Ticks;
            var connectTasks = new List<Task<BenchmarkContext>>();
            for (int n = 0; n < ccu; n++) {
                var client = ConnectClient();
                connectTasks.Add(client);
            }
            var contexts = await Task.WhenAll(connectTasks);
            
            var connected = DateTime.Now.Ticks;
            
            Console.Write($"{ccu,4} clients connected in {((connected - start) / 10000),4} ms  ");
            
            // warmup
            for (int n = 0; n < 20; n++) { 
                sender.SendMessage("test", 0);
                await sender.SyncTasks();
                await Task.Delay(10);
            }
            

            var deltaTime = 1000 / tickRate;
            
            for (int n = 0; n < tickRate; n++) { 
                sender.SendMessage("test", DateTime.Now.Ticks);   // message is published to all clients
                sender.SyncTasks();
                await Task.Delay(deltaTime);
            }
            await Task.Delay(100);

            var latencies = new List<double>();
            foreach (var c in contexts) {
                latencies.AddRange(c.latencies);
                if (c.latencies.Count != tickRate)
                    throw new InvalidOperationException("missing events");
            }
            
            
            // var diffs = contexts.Select(c => c.accumulatedLatency / (10000d * c.events)).ToArray();
            var p = GetPercentiles(latencies, 100);

            var diffStr     = $"{p[50],5:0.0} {p[95],5:0.0} {p[96],5:0.0} {p[97],5:0.0} {p[98],5:0.0} {p[99],5:0.0} {p[100],5:0.0} ";
            Console.WriteLine(diffStr);

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
        private static List<double> GetPercentiles(List<double> values, int count) {
            var sorted = values.OrderBy(s => s).ToList();
            if (sorted.Count < count) throw new InvalidOperationException("insufficient samples");
            var percentiles = new List<double>();
            percentiles.Add(0);
            for (int n = 0; n < count; n++) {
                var index = (int)((sorted.Count - 1) * ((n + 1) / (double)count));
                percentiles.Add(sorted[index]);
            }
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (sorted[sorted.Count - 1] != percentiles[count])
                throw new InvalidOperationException("invalid last element");
            return percentiles;
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
                benchmarkContext.latencies.Add((DateTime.Now.Ticks - start) / 10000d);
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
        internal    List<double>                latencies = new List<double>(); // ms
    }
}
