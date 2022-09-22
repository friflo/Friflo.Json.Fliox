using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Demo;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Remote;

// ReSharper disable UnusedVariable
namespace DemoTest {
/*                                            Benchmark results
 
clients = CCU - Concurrently Connected Users, rate = tick rate, frames = number of messages send / received events

            Hz               ms       ms     latency ms percentiles                              ms   ms/s  kb/s
clients   rate frames connected  average     50    90    95    96    97    98    99   100  duration   main alloc
      2     50    150       138      1.1    1.1   1.2   1.3   1.4   1.4   1.5   1.5  16.6      3000   10.7   124
      2     50    150         4      1.0    1.1   1.2   1.3   1.4   1.4   1.5   1.6   1.7      2999    9.7   122
      5     50    150         6      1.0    1.1   1.3   1.5   1.5   1.6   1.8   2.1   4.8      2999    9.0   123
     10     50    150         6      0.5    0.4   0.5   0.6   0.6   0.7   0.8   1.3   8.0      3000    4.6   122
     50     50    150        23      0.9    0.7   1.8   2.7   2.9   3.1   3.4   3.8   8.0      3000    5.4   122

    100     50    150        52      3.8    3.3   5.9   7.5   8.3   9.3  10.2  11.4  21.6      3000    9.8   122
    200     50    150        69      4.2    3.6   8.1   9.7  10.2  10.8  11.6  13.8  19.4      2999    6.9   122
    300     50    150       107      4.7    3.8   9.0  11.7  12.4  13.3  16.0  21.9  32.1      3000    5.4   122
    400     50    150       170      4.7    4.0   9.3  11.0  11.5  12.1  13.2  16.9  25.7      3000    4.9   122
    500     50    150       182      4.4    3.9   8.5  10.5  10.9  11.4  12.4  14.6  21.4      3005    4.3   122

   1000     50    150       451     10.0    8.8  19.1  21.3  22.8  25.1  26.9  28.6  41.8      3008    4.1   122
   2000     50    150      1126     73.4   65.2 132.9 155.1 161.7 166.6 172.7 188.6 226.0      3015    9.1   123

   3000     10     30      1603     42.2   42.3  72.4  88.7  92.7  94.4  95.9  99.6 128.0      3004    1.0    25
   5000      5     15      2186     82.4   79.3 142.0 148.0 149.7 179.6 182.5 210.6 307.9      3019    0.6    12
  10000      2      6      4102    191.6  188.8 309.7 353.7 383.1 455.1 460.4 492.8 496.7      3064    0.2     5
  15000      2      6      6364    390.1  384.8 628.4 671.1 675.5 702.4 706.9 733.2 739.6      3058    9.0     6

      2  10000  10000         1    423.9  430.7 728.0 766.2 772.3 780.2 788.2 795.9 806.6      1001  246.6 25533
      2  20000  20000         1    1313.3  1321.5 2318.8 2436.2 2454.4 2476.2 2509.6 2542.9 2569.9      1000  432.5 51654
 */
    internal static class Benchmark
    {
        internal static async Task PubSubLatency()
        {
            var hub     = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
            var sender  = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            
            var rate    = 50; // tick rate
            var frames  = 150;
            Console.WriteLine(@"
--- Benchmark ---
1. one client send a message to host
2. the host send this message to all subscribed clients
3. subscribed clients receive message event

All latencies from 1 to 3 are recorded and percentiles are calculated.

clients = CCU - Concurrently Connected Users
rate    = tick rate
frames  = number of messages send / received events
");
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
