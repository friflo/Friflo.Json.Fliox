using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.GraphQL;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Misc;

// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Tests.Main
{
    internal  static partial class  Program
    {
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
        private static void FlioxServer(string endpoint) {
            var hostHub = CreateHttpHost();
        //  var hostHub = CreateMiniHost();
            var server = new HttpListenerHost(endpoint, hostHub);
            server.Start();
            server.Run();
        }
        
        /// <summary>
        /// Blueprint method showing how to setup a <see cref="HttpHostHub"/> utilizing all features available
        /// via HTTP and WebSockets.
        /// </summary>
        public static HttpHostHub CreateHttpHost() {
            var c                   = new Config();
            var typeSchema          = new NativeTypeSchema(typeof(PocStore)); // optional - create TypeSchema from Type 
        //  var typeSchema          = CreateTypeSchema();               // alternatively create TypeSchema from JSON Schema
            var databaseSchema      = new DatabaseSchema(typeSchema);
            var database            = CreateDatabase(c, databaseSchema, new PocHandler());
            
            var hub                 = new FlioxHub(database);
            hub.Info.projectName    = "Test Hub";                                                               // optional
            hub.Info.projectWebsite = "https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Main";  // optional
            hub.Info.envName        = "dev"; hub.Info.envColor = "rgb(34 140 0)";                               // optional
            hub.AddExtensionDB (ClusterDB.Name, new ClusterDB(hub));    // optional - expose info of hosted databases. Required by Hub Explorer
            hub.AddExtensionDB (MonitorDB.Name, new MonitorDB(hub));    // optional - expose monitor stats as extension database
            hub.EventBroker         = new EventBroker(true);            // optional - enables sending events for subscriptions
            
            var userDB              = new FileDatabase(c.userDbPath, new UserDBHandler(), null, false);
            hub.Authenticator       = new UserAuthenticator(userDB);    // optional - otherwise all request tasks are authorized
            hub.AddExtensionDB("user_db", userDB);                      // optional - expose userStore as extension database
            
            var hostHub             = new HttpHostHub(hub, "/fliox/").CacheControl(c.cache);
            hostHub.AddHandler       (new GraphQLHandler());
            hostHub.AddHandler       (new StaticFileHandler(c.www).CacheControl(c.cache)); // optional - serve static web files of Hub Explorer
            hostHub.AddSchemaGenerator("jtd", "JSON Type Definition", JsonTypeDefinition.GenerateJTD);  // optional - add code generator
            return hostHub;
        }
        
        class Config {
            internal readonly string  dbPath      = "./Json.Tests/assets~/DB/PocStore";
            internal readonly string  userDbPath  = "./Json.Tests/assets~/DB/UserStore";
            internal readonly string  www         = "./Json/Fliox.Hub.Explorer/www~"; // HubExplorer.Path;
            internal readonly string  cache       = null; // "max-age=600"; // HTTP Cache-Control
            internal readonly bool    useMemoryDbClone    = false;
        }
        
        private static HttpHostHub CreateMiniHost() {
            var c                   = new Config();
            // Run a minimal Fliox server without monitoring, messaging, Pub-Sub, user authentication / authorization & entity validation
            var database            = CreateDatabase(c, null, new PocHandler());
            var hub          	    = new FlioxHub(database);
            var hostHub             = new HttpHostHub(hub, "/fliox/");
            hostHub.AddHandler       (new StaticFileHandler(c.www, c.cache));   // optional - serve static web files of Hub Explorer
            return hostHub;
        }
        
        private static EntityDatabase CreateDatabase(Config c, DatabaseSchema schema, TaskHandler handler) {
            var fileDb = new FileDatabase(c.dbPath, handler, null, false);
            fileDb.Schema = schema;
            if (!c.useMemoryDbClone)
                return fileDb;
            var memoryDB = new MemoryDatabase(handler);
            memoryDB.Schema = schema;
            memoryDB.SeedDatabase(fileDb).Wait();
            return memoryDB;
        }
        
        private static TypeSchema CreateTypeSchema() {
            var schemas = JsonTypeSchema.ReadSchemas("./Json.Tests/assets~/Schema/JSON/PocStore");
            return new JsonTypeSchema(schemas, "./UnitTest.Fliox.Client.json#/definitions/PocStore");
        }
    }
}
