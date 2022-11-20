using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;

namespace Friflo.Json.Tests.Main
{
    public static class Throughput
    {
        private const string TestDB = "test_db";
        
        public static async Task MemoryDbThroughput() {
            var database    = new MemoryDatabase(TestDB);
            var hub         = new FlioxHub(database);
            await TestHappy.ConcurrentAccess(hub, 4, 0, 10_000_000, false);
        }
        
        public static async Task FileDbThroughput() {
            var database    = new FileDatabase(TestDB, "./Json.Tests/assets~/DB/test_concurrency_db");
            var hub         = new FlioxHub(database);
            await TestHappy.ConcurrentAccess(hub, 4, 0, 1_000_000, false);
        }
        
        public static async Task WebsocketDbThroughput() {
            using (var database     = new MemoryDatabase(TestDB))
            using (var hub          = new FlioxHub(database))
            using (var httpHost     = new HttpHost(hub, "/"))
            using (var server       = new HttpListenerHost("http://+:8080/", httpHost))
            using (var remoteHub    = new WebSocketClientHub(TestDB, "ws://localhost:8080/")) {
                await TestHappy.RunServer(server, async () => {
                    // await remoteHub.Connect();
                    await TestHappy.ConcurrentAccess(remoteHub, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task HttpDbThroughput() {
            using (var database     = new MemoryDatabase(TestDB))
            using (var hub          = new FlioxHub(database))
            using (var httpHost     = new HttpHost(hub, "/"))
            using (var server       = new HttpListenerHost("http://+:8080/", httpHost))
            using (var remoteHub    = new HttpClientHub(TestDB, "ws://localhost:8080/")) {
                await TestHappy.RunServer(server, async () => {
                    await TestHappy.ConcurrentAccess(remoteHub, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task LoopbackDbThroughput() {
            var database                = new MemoryDatabase(TestDB);
            using (var hub          	= new FlioxHub(database))
            using (var loopbackHub      = new LoopbackHub(hub)) {
                await TestHappy.ConcurrentAccess(loopbackHub, 4, 0, 1_000_000, false);
            }
        }
    }
}
