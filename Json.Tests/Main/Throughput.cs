using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;

namespace Friflo.Json.Tests.Main
{
    public static class Throughput
    {
        public static async Task MemoryDbThroughput() {
            var database    = new MemoryDatabase();
            var hub         = new DatabaseHub(database);
            await TestStore.ConcurrentAccess(hub, 4, 0, 1_000_000, false);
        }
        
        public static async Task FileDbThroughput() {
            var database    = new FileDatabase("./Json.Tests/assets~/DB/testConcurrencyDb");
            var hub         = new DatabaseHub(database);
            await TestStore.ConcurrentAccess(hub, 4, 0, 1_000_000, false);
        }
        
        public static async Task WebsocketDbThroughput() {
            using (var database         = new MemoryDatabase())
            using (var hub          	= new DatabaseHub(database))
            using (var hostDatabase     = new HttpHostHub(hub))
            using (var server           = new HttpListenerHost("http://+:8080/", hostDatabase))
            using (var remoteDatabase   = new WebSocketClientHub("ws://localhost:8080/")) {
                await TestStore.RunServer(server, async () => {
                    await remoteDatabase.Connect();
                    await TestStore.ConcurrentAccess(remoteDatabase, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task HttpDbThroughput() {
            using (var database         = new MemoryDatabase())
            using (var hub          	= new DatabaseHub(database))
            using (var hostDatabase     = new HttpHostHub(hub))
            using (var server           = new HttpListenerHost("http://+:8080/", hostDatabase))
            using (var remoteDatabase   = new HttpClientHub("ws://localhost:8080/")) {
                await TestStore.RunServer(server, async () => {
                    await TestStore.ConcurrentAccess(remoteDatabase, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task LoopbackDbThroughput() {
            var database                = new MemoryDatabase();
            using (var hub          	= new DatabaseHub(database))
            using (var loopbackDatabase = new LoopbackHub(hub)) {
                await TestStore.ConcurrentAccess(loopbackDatabase, 4, 0, 1_000_000, false);
            }
        }
    }
}
