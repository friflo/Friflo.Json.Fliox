using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Demo;
using Friflo.Json.Fliox.Hub.Client;

namespace DemoTest {

    internal static class Benchmark
    {
        internal static async Task PubSubLatency()
        {
            await PubSubLatencyCCU(1);
            await PubSubLatencyCCU(1);
            await PubSubLatencyCCU(10);
            await PubSubLatencyCCU(100);
            await PubSubLatencyCCU(1000);
            await PubSubLatencyCCU(10000);
        }
        
        private static async Task PubSubLatencyCCU(int ccu)
        {
            var start = DateTime.Now;
            var connectTasks = new List<Task<BenchmarkContext>>();
            for (int n = 0; n < ccu; n++) {
                var client = ConnectClient();
                connectTasks.Add(client);
            }
            var contexts = await Task.WhenAll(connectTasks);
            
            var connected = DateTime.Now;
            
            Console.WriteLine($"{ccu} clients connected. {(connected - start).TotalMilliseconds} ms");
            
            var tasks = new List<Task>(); 
            foreach (var context in contexts) {
                context.client.SendMessage("test", 1337);
                var task = context.client.SyncTasks();
                
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            foreach (var context in contexts) {
                context.client.Dispose();
            }
        }
        
        private static async Task<BenchmarkContext> ConnectClient()
        {
            var hub         = Trial.CreateHub("ws");
            var client      = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            
            client.SubscribeMessage("*", (message, context) => {
                
            });
            await client.SyncTasks();
            
            return new BenchmarkContext {
                client = client
            };
        }
    }
    
    internal class BenchmarkContext
    {
        internal FlioxClient client;
    }
}
