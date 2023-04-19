using System.Net;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.GraphQL;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Native;
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
            var typeSchema          = NativeTypeSchema.Create(typeof(TestClient)); // optional - create TypeSchema from Type 
            var databaseSchema      = new DatabaseSchema(typeSchema);
            var fileDb              = Env.CreateFileDatabase(databaseSchema);
            var memoryDb            = await Env.CreateMemoryDatabase(fileDb);
            
            var hub                 = new FlioxHub(memoryDb, env) { HostName = "test-server" };
            hub.Info.projectName    = "Test DB";                                                                // optional
            hub.Info.projectWebsite = "https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Main";  // optional
            hub.Info.envName        = "test"; hub.Info.envColor = "rgb(0 140 255)";                              // optional
            hub.AddExtensionDB (fileDb);
            if (Env.TEST_DB == "cosmos") {
                var testDb              = await Env.CreateCosmosDatabase(fileDb);
                hub.AddExtensionDB (testDb);
            }
            hub.AddExtensionDB (new ClusterDB("cluster", hub));         // optional - expose info of hosted databases. Required by Hub Explorer
            hub.AddExtensionDB (new MonitorDB("monitor", hub));         // optional - expose monitor stats as extension database
            hub.EventDispatcher     = new EventDispatcher(EventDispatching.QueueSend, env); // optional - enables Pub-Sub (sending events for subscriptions)
            
        /*    
            var userDB              = new FileDatabase("user_db", c.UserDbPath, new UserDBService()) { Pretty = false };
            var userAuthenticator   = new UserAuthenticator(userDB, env);
            await userAuthenticator.SetAdminPermissions();                                  // optional - enable Hub access with user/token: admin/admin
            await userAuthenticator.SubscribeUserDbChanges(hub.EventDispatcher);            // optional - apply user_db changes instantaneously
            hub.AddExtensionDB(userDB);                                                     // optional - expose userStore as extension database
            hub.Authenticator       = userAuthenticator;                                    // optional - otherwise all request tasks are authorized
        */    
            var httpHost            = new HttpHost(hub, "/fliox/", env)       { CacheControl = cache };
            httpHost.AddHandler      (new GraphQLHandler());
            httpHost.AddHandler      (new StaticFileHandler(HubExplorer.Path) { CacheControl = cache }); // optional - serve static web files of Hub Explorer
            return httpHost;
        }
    }
}