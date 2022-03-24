using System.Linq;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Native;

namespace Fliox.DemoHub
{
    /// <summary>
    /// Bootstrapping of databases hosted by <see cref="HttpHostHub"/>
    /// </summary> 
    internal  static class  Program
    {
        public static void Main(string[] args) {
            if (args.Contains("HttpListener")) {
                RunHttpListener("http://+:8010/");
                return;
            }
            Startup.RunAspNetCore(args);
        }

        // Example requests for server at: /Json.Tests/www~/example-requests/
        //
        //   Note:
        // Http server may require a permission to listen to the given host/port.
        // Otherwise exception is thrown on startup: System.Net.HttpListenerException: permission denied.
        // To give access see: [add urlacl - Win32 apps | Microsoft Docs] https://docs.microsoft.com/en-us/windows/win32/http/add-urlacl
        //     netsh http add urlacl url=http://+:8010/ user=<DOMAIN>\<USER> listen=yes
        //     netsh http delete urlacl http://+:8010/
        // 
        // Get DOMAIN\USER via  PowerShell
        //     $env:UserName
        //     $env:UserDomain 
        private static void RunHttpListener(string endpoint) {
            var hostHub = CreateHttpHost();
        //  var hostHub = CreateMiniHost();
            var server = new HttpListenerHost(endpoint, hostHub);
            server.Start();
            server.Run();
        }
        
        /// <summary>
        /// This method is a blueprint showing how to setup a <see cref="HttpHostHub"/> utilizing all features available
        /// via HTTP and WebSockets. The Hub can be integrated by two different HTTP servers:
        /// <list type="bullet">
        ///   <item> By <see cref="System.Net.HttpListener"/> see <see cref="RunHttpListener"/> </item>
        ///   <item> By <a href="https://docs.microsoft.com/en-us/aspnet/core/">ASP.NET Core / Kestrel</a> see <see cref="Startup.Configure"/></item>
        /// </list>
        /// The features of a <see cref="HttpHostHub"/> instance utilized by this blueprint method are listed at
        /// <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Fliox.Hub/Host/README.md#httphosthub">Host/README.md</a><br/>
        /// <i>Note</i>: All extension databases added by <see cref="FlioxHub.AddExtensionDB"/> could be exposed by an
        /// additional <see cref="HttpHostHub"/> instance only accessible from Intranet as they contains sensitive data.
        /// </summary>
        internal static HttpHostHub CreateHttpHost() {
            var c                   = new Config();
            var typeSchema          = new NativeTypeSchema(typeof(DemoStore)); // optional - create TypeSchema from Type
            var databaseSchema      = new DatabaseSchema(typeSchema);
            var database            = CreateDatabase(c, databaseSchema, new MessageHandler());

            var hub                 = new FlioxHub(database);
            hub.Info.projectName    = "DemoHub";                                                        // optional
            hub.Info.projectWebsite = "https://github.com/friflo/Friflo.Json.Fliox/tree/main/DemoHub";  // optional
            hub.Info.envName        = "dev";                                                            // optional
            hub.AddExtensionDB (ClusterDB.Name, new ClusterDB(hub));    // optional - expose info of hosted databases. Required by Hub Explorer
            hub.AddExtensionDB (MonitorDB.Name, new MonitorDB(hub));    // optional - expose monitor stats as extension database
            hub.EventBroker         = new EventBroker(true);            // optional - eventBroker enables Instant Messaging & Pub-Sub
            
            var userDB              = new FileDatabase(c.userDbPath, new UserDBHandler(), null, false);
            hub.Authenticator       = new UserAuthenticator(userDB);    // optional - otherwise all request tasks are authorized
            hub.AddExtensionDB("user_db", userDB);                      // optional - expose userStore as extension database
            
            var hostHub             = new HttpHostHub(hub).CacheControl(c.cache);
            hostHub.AddHandler       (new StaticFileHandler(c.www).CacheControl(c.cache)); // optional - serve static web files of Hub Explorer
            return hostHub;
        }
        
        private class Config {
            internal readonly string  dbPath              = "./DB~/DemoStore";
            internal readonly string  userDbPath          = "./DB~/UserStore";
            internal readonly string  www                 = HubExplorer.Path; // "../Json/Fliox.Hub.Explorer/www~";
            internal readonly string  cache               = null; // "max-age=600"; // HTTP Cache-Control
            internal readonly bool    useMemoryDbClone    = true;
        }
        
        private static EntityDatabase CreateDatabase(Config c, DatabaseSchema schema, TaskHandler handler) {
            var fileDb = new FileDatabase(c.dbPath, handler);
            fileDb.Schema = schema;
            if (!c.useMemoryDbClone)
                return fileDb;
            // As the DemoHub is also deployed as a demo service in the internet it uses a memory database
            // to minimize operation cost and prevent abuse as a free persistent database.   
            var memoryDB = new MemoryDatabase(handler);
            memoryDB.Schema = schema;
            memoryDB.SeedDatabase(fileDb).Wait();
            return memoryDB;
        }
    }
}
