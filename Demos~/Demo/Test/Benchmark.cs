using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Demo;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Remote;

// ReSharper disable UnusedVariable
namespace DemoTest {

    internal static class Benchmark
    {
        internal static async Task PubSubLatency()
        {
            var hub     = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
            var sender  = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            
            var rate    = 50; // tick rate
            var frames  = 150;
            Console.WriteLine("clients = CCU - Concurrently Connected Users, rate = tick rate, frames = number of messages send / received events");
            Console.WriteLine();
            Console.WriteLine("            Hz               ms       ms     latency ms percentiles                              ms   ms/s  kb/s");
            Console.WriteLine("clients   rate frames connected  average     50    90    95    96    97    98    99   100  duration   main alloc");
            
            await PubSubLatencyCCU(sender,     2,   rate, frames);
            await PubSubLatencyCCU(sender,     2,   rate, frames);
            await PubSubLatencyCCU(sender,     5,   rate, frames);
            await PubSubLatencyCCU(sender,    10,   rate, frames);
            await PubSubLatencyCCU(sender,    50,   rate, frames);
            Console.WriteLine();
            await PubSubLatencyCCU(sender,   100,   rate, frames);
            await PubSubLatencyCCU(sender,   200,   rate, frames);
            await PubSubLatencyCCU(sender,   300,   rate, frames);
            await PubSubLatencyCCU(sender,   400,   rate, frames);
            await PubSubLatencyCCU(sender,   500,   rate, frames);
            Console.WriteLine();
            await PubSubLatencyCCU(sender,  1000,   rate, frames);
            await PubSubLatencyCCU(sender,  2000,   rate, frames);
            Console.WriteLine();
            await PubSubLatencyCCU(sender,  3000,     10,     30);
            await PubSubLatencyCCU(sender,  5000,      5,     15);
            await PubSubLatencyCCU(sender, 10000,      2,      6);
            await PubSubLatencyCCU(sender, 15000,      2,      6);
            Console.WriteLine();
            await PubSubLatencyCCU(sender,     2,  10000,  10000);
            await PubSubLatencyCCU(sender,     2,  20000,  20000);
        }
        
        const string payload_100 = "_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789";
        
        private static async Task PubSubLatencyCCU(FlioxClient sender, int ccu, int tickRate, int frames)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            Console.Write($"{ccu,7} {tickRate,6} {frames,6} ");
            
            var start = DateTime.Now.Ticks;
            var connectTasks = new List<Task<BenchmarkContext>>();
            for (int n = 0; n < ccu; n++) {
                var client = ConnectClient(frames);
                connectTasks.Add(client);
            }
            var contexts = await Task.WhenAll(connectTasks);
            
            var connected = DateTime.Now.Ticks;
            Console.Write($"   {(connected - start) / 10000,6}  ");
            
            // warmup
            for (int n = 0; n < 20; n++) { 
                sender.SendMessage<TestMessage>("test", null);
                await sender.SyncTasks();
                await Task.Delay(10);
            }
            var deltaTime = 1000d / tickRate;
            
            long    sendTicks   = 0;
            long    bytes       = 0;
            start = DateTime.Now.Ticks;
            var payload = payload_100;
            for (int n = 1; n <= frames; n++) {
                var msgStart    = DateTime.Now.Ticks;
                var bytesStart  = GC.GetAllocatedBytesForCurrentThread();
                var testMessage = new TestMessage { start = DateTime.Now.Ticks, payload = payload};
                sender.SendMessage("test", testMessage);   // message is published to all clients
                var noAwait = sender.SyncTasks();
                sendTicks  += DateTime.Now.Ticks - msgStart;
                bytes      += GC.GetAllocatedBytesForCurrentThread() - bytesStart;
                
                var delay = n * deltaTime - (DateTime.Now.Ticks - start) / 10000d;
                if (delay > 0) {
                    await Task.Delay((int)delay);
                }
            }
            var duration        = (DateTime.Now.Ticks - start) / 10000;
            var sendDuration    = tickRate * sendTicks / (frames * 10000d);
            var kiloBytesPerSec = tickRate * bytes     / (frames * 1000);
            
            var receiveAllTasks = contexts.Select(bc => bc.tcs.Task);
            await Task.WhenAll(receiveAllTasks);

            var latencies = new List<double>();
            
            foreach (var c in contexts) {
                latencies.AddRange(c.latencies);
                if (c.latencies.Count != frames)
                    throw new InvalidOperationException("missing events");
            }

            // var diffs = contexts.Select(c => c.accumulatedLatency / (10000d * c.events)).ToArray();
            var p   = GetPercentiles(latencies, 100);
            var avg = latencies.Average();

            var diffStr     = $"  {avg,5:0.0}  {p[50],5:0.0} {p[90],5:0.0} {p[95],5:0.0} {p[96],5:0.0} {p[97],5:0.0} {p[98],5:0.0} {p[99],5:0.0} {p[100],5:0.0} ";
            Console.WriteLine($"{diffStr}    {duration,5} {sendDuration,6:0.0} {kiloBytesPerSec,5}");

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
        
        private static async Task<BenchmarkContext> ConnectClient(int frames)
        {
            var hub     = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
            var client  = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            var bc      = new BenchmarkContext { hub = hub, client = client, frames = frames };
            
            client.SubscribeMessage("*", (message, context) => {
                message.GetParam(out TestMessage test, out _);
                if (test == null)
                    return;
                bc.latencies.Add((DateTime.Now.Ticks - test.start) / 10000d);
                if (bc.latencies.Count == bc.frames) {
                    bc.tcs.SetResult();
                }
            });
            await client.SyncTasks();
            
            return bc;
        }
    }
    
    internal class BenchmarkContext
    {
        internal            WebSocketClientHub      hub;
        internal            FlioxClient             client;
        internal readonly   List<double>            latencies = new List<double>(); // ms
        internal readonly   TaskCompletionSource    tcs = new TaskCompletionSource();
        internal            int                     frames;
    }
    
    internal class TestMessage {
        public  long    start;
        public  string  payload;
    }
}
