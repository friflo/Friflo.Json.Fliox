#if !UNITY_2020_1_OR_NEWER

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Database;
using Friflo.Json.Fliox.DB.Database.Remote;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Graph;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Graph.Happy;

namespace Friflo.Json.Tests.Main
{
    public static class Throughput
    {
        public static async Task MemoryDbThroughput() {
            var db = new MemoryDatabase();
            await TestStore.ConcurrentAccess(db, 4, 0, 1_000_000, false);
        }
        
        public static async Task FileDbThroughput() {
            var db = new FileDatabase("./Json.Tests/assets~/Graph/testConcurrencyDb");
            await TestStore.ConcurrentAccess(db, 4, 0, 1_000_000, false);
        }
        
        public static async Task WebsocketDbThroughput() {
            var db = new MemoryDatabase();
            using (var hostDatabase     = new HttpHostDatabase(db, "http://+:8080/", null))
            using (var remoteDatabase   = new WebSocketClientDatabase("ws://localhost:8080/")) {
                await TestStore.RunRemoteHost(hostDatabase, async () => {
                    await remoteDatabase.Connect();
                    await TestStore.ConcurrentAccess(remoteDatabase, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task HttpDbThroughput() {
            var db = new MemoryDatabase();
            using (var hostDatabase     = new HttpHostDatabase(db, "http://+:8080/", null))
            using (var remoteDatabase   = new HttpClientDatabase("ws://localhost:8080/")) {
                await TestStore.RunRemoteHost(hostDatabase, async () => {
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

#endif