using System.Net;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Tests.DB.Client;

namespace Friflo.Json.Tests.DB
{
    public class Program
    {
      
        public static async Task Run()
        {
            var host            = await CreateHttpHost();
            var httpListener    = new HttpListener();
            httpListener.Prefixes.Add("http://+:8011/");
            var server          = new HttpServer(httpListener, host);
            server.Start();
            server.Run();
        }
        
        private static async Task<HttpHost> CreateHttpHost() {
            var env                 = new SharedEnv();
            string      cache       = null;
            var databaseSchema      = new DatabaseSchema(typeof(TestClient));
            var fileDb              = Env.CreateFileDatabase(databaseSchema);
            var memoryDb            = await Env.CreateMemoryDatabase(fileDb);
            
            var hub                 = new FlioxHub(memoryDb, env);
            hub.Info.projectName    = "Test DB";
            hub.Info.projectWebsite = "https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/DB";
            hub.Info.envName        = "test"; hub.Info.envColor = "rgb(0 140 255)";
            hub.AddExtensionDB (fileDb);
            if (Env.TEST_DB == "cosmos") {
                var testDb              = await Env.CreateCosmosDatabase(fileDb);
                hub.AddExtensionDB (testDb);
            }
            hub.AddExtensionDB (new ClusterDB("cluster", hub));         // optional - expose info of hosted databases. Required by Hub Explorer
            hub.AddExtensionDB (new MonitorDB("monitor", hub));         // optional - expose monitor stats as extension database
            hub.EventDispatcher     = new EventDispatcher(EventDispatching.QueueSend, env); // optional - enables Pub-Sub (sending events for subscriptions)
            
            var httpHost            = new HttpHost(hub, "/fliox/", env)       { CacheControl = cache };
            httpHost.AddHandler      (new StaticFileHandler(HubExplorer.Path) { CacheControl = cache }); // optional - serve static web files of Hub Explorer
            return httpHost;
        }
    }
}