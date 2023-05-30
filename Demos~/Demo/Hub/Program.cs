using System.Linq;
using System.Threading.Tasks;
using Demo;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.GraphQL;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;

namespace DemoHub
{
    /// <summary>Bootstrapping of databases hosted by <see cref="HttpHost"/></summary> 
    internal  static class  Program
    {
        public static void Main(string[] args)
        {
            if (args.Contains("HttpListener")) {
                var httpHost = CreateHttpHost().Result;
                HttpServer.RunHost("http://+:8010/", httpHost);
                return;
            }
            // Startup.Run(args);   // ASP.NET Core 3, 3.1, 5
            StartupAsp6.Run(args);  // ASP.NET Core 6
        }

        /// <summary>
        /// This method is a blueprint showing how to setup a <see cref="HttpHost"/> utilizing all features available
        /// via HTTP and WebSockets. The Hub can be integrated by two different HTTP servers:
        /// <list type="bullet">
        ///   <item> By <see cref="System.Net.HttpListener"/> see <see cref="HttpServer.RunHost"/> </item>
        ///   <item> By <a href="https://docs.microsoft.com/en-us/aspnet/core/">ASP.NET Core / Kestrel</a> see <see cref="Startup.Configure"/></item>
        /// </list>
        /// The features of a <see cref="HttpHost"/> instance utilized by this blueprint method are listed at
        /// <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Fliox.Hub/Host/README.md#httphost">Host README.md</a><br/>
        /// <i>Note</i>: All extension databases added by <see cref="FlioxHub.AddExtensionDB"/> could be exposed by an
        /// additional <see cref="HttpHost"/> instance only accessible from Intranet as they contains sensitive data.
        /// </summary>
        internal static async Task<HttpHost> CreateHttpHost()
        {
            var databaseSchema      = DatabaseSchema.Create<DemoClient>();           // optional - create TypeSchema from Type
            var database            = CreateDatabase(databaseSchema).AddCommands(new DemoCommands());
            var hub                 = new FlioxHub(database);
            hub.Info.projectName    = "DemoHub";                                        // optional
            hub.Info.projectWebsite = "https://github.com/friflo/Fliox.Examples#demo";  // optional
            hub.Info.envName        = "dev"; hub.Info.envColor = "rgb(0 171 145)";      // optional
            hub.AddExtensionDB (new ClusterDB("cluster", hub));     // optional - expose info of hosted databases. cluster is required by Hub Explorer
            hub.AddExtensionDB (new MonitorDB("monitor", hub));     // optional - expose monitor stats as extension database
            hub.EventDispatcher     = new EventDispatcher(EventDispatching.QueueSend);   // optional - enables Pub-Sub (sending events for subscriptions)
            
            var userDB              = new FileDatabase("user_db", "../Test/DB/user_db", new UserDBService()) { Schema = UserDB.Schema, Pretty = false };
            var authenticator       = new UserAuthenticator(userDB);
            await authenticator.SetAdminPermissions();                                  // optional - enable Hub access with user/token: admin/admin
            await authenticator.SetClusterPermissions("cluster", Users.All);
            await authenticator.SubscribeUserDbChanges(hub.EventDispatcher);            // optional - apply user_db changes instantaneously
            hub.AddExtensionDB(userDB);                                                 // optional - expose user_db as extension database
            hub.Authenticator       = authenticator;                                    // optional - otherwise all tasks are authorized
            
            var httpHost            = new HttpHost(hub, "/fliox/");
            httpHost.AddHandler      (new GraphQLHandler());
            httpHost.AddHandler      (new StaticFileHandler(HubExplorer.Path)); // optional - serve static web files of Hub Explorer
            // httpHost.AddHandler      (new StaticFileHandler("www", typeof(Program)));
            httpHost.AddHandler      (new StaticFileHandler("www") { CacheControl = null });
            // CreateWebRtcServer(httpHost).Wait();
            return httpHost;
        }
        
        /* private static async Task CreateWebRtcServer(HttpHost httpHost) {
            var rtcConfig = new SignalingConfig {
                SignalingHost   = "ws://localhost:8011/fliox/",
                User            = "admin", Token = "admin",
                WebRtcConfig    = new WebRtcConfig { IceServerUrls = new [] { "stun:stun.sipsorcery.com" } },
            };
            var rtcServer = new RtcServer(rtcConfig);
            await rtcServer.AddHost("abc", httpHost);
        } */
        
        private static readonly bool UseMemoryDbClone = true;
        
        private static EntityDatabase CreateDatabase(DatabaseSchema schema)
        {
            var fileDb = new FileDatabase("main_db", "../Test/DB/main_db") { Schema = schema };
            if (!UseMemoryDbClone)
                return fileDb;
            // As the DemoHub is also deployed as a demo service in the internet it uses a memory database
            // to minimize operation cost and prevent abuse as a free persistent database.   
            var memoryDB = new MemoryDatabase("main_db") { Schema = schema };
            memoryDB.SeedDatabase(fileDb).Wait();
            return memoryDB;
        }
    }
}
