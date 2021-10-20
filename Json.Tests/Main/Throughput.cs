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
            var db = new MemoryDatabase();
            await TestStore.ConcurrentAccess(db, 4, 0, 1_000_000, false);
        }
        
        public static async Task FileDbThroughput() {
            var db = new FileDatabase("./Json.Tests/assets~/DB/testConcurrencyDb");
            await TestStore.ConcurrentAccess(db, 4, 0, 1_000_000, false);
        }
        
        public static async Task WebsocketDbThroughput() {
            using (var database         = new MemoryDatabase())
            using (var hostDatabase     = new HttpHostDatabase(database))
            using (var server           = new HttpListenerHost("http://+:8080/", hostDatabase))
            using (var remoteDatabase   = new WebSocketClientDatabase("ws://localhost:8080/")) {
                await TestStore.RunServer(server, async () => {
                    await remoteDatabase.Connect();
                    await TestStore.ConcurrentAccess(remoteDatabase, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task HttpDbThroughput() {
            using (var database         = new MemoryDatabase())
            using (var hostDatabase     = new HttpHostDatabase(database))
            using (var server           = new HttpListenerHost("http://+:8080/", hostDatabase))
            using (var remoteDatabase   = new HttpClientDatabase("ws://localhost:8080/")) {
                await TestStore.RunServer(server, async () => {
                    await TestStore.ConcurrentAccess(remoteDatabase, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task LoopbackDbThroughput() {
            var db = new MemoryDatabase();
            using (var loopbackDatabase     = new LoopbackDatabase(db)) {
                await TestStore.ConcurrentAccess(loopbackDatabase, 4, 0, 1_000_000, false);
            }
        }
    }
}
