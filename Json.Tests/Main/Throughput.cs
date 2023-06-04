using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Remote.Transport.Udp;
using Friflo.Json.Fliox.Hub.SQLite;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using Friflo.Json.Tests.Common.Utils;


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
        
#if !UNITY_5_3_OR_NEWER || SQLITE
        public static async Task SQLiteThroughput() {
            var database    = new SQLiteDatabase(TestDB, CommonUtils.GetBasePath() + "test_concurrency_db.sqlite3", null);
            var hub         = new FlioxHub(database);
            await TestHappy.ConcurrentAccess(hub, 4, 0, 1_000_000, false);
        }
#endif
        
        public static async Task FileDbThroughput() {
            var database    = new FileDatabase(TestDB, "./Json.Tests/assets~/DB/test_concurrency_db");
            var hub         = new FlioxHub(database);
            await TestHappy.ConcurrentAccess(hub, 4, 0, 1_000_000, false);
        }
        
        public static async Task WebsocketDbThroughput() {
            using (var database     = new MemoryDatabase(TestDB))
            using (var hub          = new FlioxHub(database))
            using (var httpHost     = new HttpHost(hub, "/"))
            using (var server       = new HttpServer("http://+:8080/", httpHost))
            using (var remoteHub    = new WebSocketClientHub(TestDB, "ws://localhost:8080/")) {
                hub.GetFeature<RemoteHostEnv>().logMessages = false;
                remoteHub.ClientEnv.logMessages             = false;
                await TestHappy.RunServer(server, async () => {
                    // await remoteHub.Connect();
                    await TestHappy.ConcurrentAccess(remoteHub, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task UdpDbThroughput() {
            // SharedEnv.Default.Logger = new HubLoggerStream("hub.log");
            using (var database     = new MemoryDatabase(TestDB))
            using (var hub          = new FlioxHub(database))
            using (var server       = new UdpServerSync("127.0.0.1:5000", hub, 3))
            using (var remoteHub    = new UdpSocketClientHub(TestDB, "127.0.0.1:5000")) {
                hub.GetFeature<RemoteHostEnv>().logMessages = false;
                remoteHub.ClientEnv.logMessages             = false;
                await TestHappy.RunServer(server, async () => {
                    await TestHappy.ConcurrentAccess(remoteHub, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task HttpDbThroughput() {
            using (var database     = new MemoryDatabase(TestDB))
            using (var hub          = new FlioxHub(database))
            using (var httpHost     = new HttpHost(hub, "/"))
            using (var server       = new HttpServer("http://+:8080/", httpHost))
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
