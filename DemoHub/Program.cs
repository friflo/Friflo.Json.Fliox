using System.Linq;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.GraphQL;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Native;

namespace Fliox.DemoHub
{
    /// <summary>Bootstrapping of databases hosted by <see cref="HttpHost"/></summary> 
    internal  static class  Program
    {
        public static void Main(string[] args) {
            if (args.Contains("HttpListener")) {
                var hostHub = CreateHttpHost();
                HttpListenerHost.RunHost("http://+:8010/", hostHub);
                return;
            }
            Startup.RunAspNetCore(args);
        }

        /// <summary>
        /// This method is a blueprint showing how to setup a <see cref="HttpHost"/> utilizing all features available
        /// via HTTP and WebSockets. The Hub can be integrated by two different HTTP servers:
        /// <list type="bullet">
        ///   <item> By <see cref="System.Net.HttpListener"/> see <see cref="HttpListenerHost.RunHost"/> </item>
        ///   <item> By <a href="https://docs.microsoft.com/en-us/aspnet/core/">ASP.NET Core / Kestrel</a> see <see cref="Startup.Configure"/></item>
        /// </list>
        /// The features of a <see cref="HttpHost"/> instance utilized by this blueprint method are listed at
        /// <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Fliox.Hub/Host/README.md#httphost">Host README.md</a><br/>
        /// <i>Note</i>: All extension databases added by <see cref="FlioxHub.AddExtensionDB"/> could be exposed by an
        /// additional <see cref="HttpHost"/> instance only accessible from Intranet as they contains sensitive data.
        /// </summary>
        internal static HttpHost CreateHttpHost() {
            var c                   = new Config();
            var typeSchema          = NativeTypeSchema.Create(typeof(DemoStore)); // optional - create TypeSchema from Type
            var databaseSchema      = new DatabaseSchema(typeSchema);
            var database            = CreateDatabase(c, databaseSchema, new MessageHandler());

            var hub                 = new FlioxHub(database);
            hub.Info.projectName    = "DemoHub";                                                        // optional
            hub.Info.projectWebsite = "https://github.com/friflo/Friflo.Json.Fliox/tree/main/DemoHub";  // optional
            hub.Info.envName        = "dev";                                                            // optional
            hub.AddExtensionDB (new ClusterDB("cluster", hub));     // optional - expose info of hosted databases. Required by Hub Explorer
            hub.AddExtensionDB (new MonitorDB("monitor", hub));     // optional - expose monitor stats as extension database
            hub.EventDispatcher     = new EventDispatcher(true);    // optional - enables sending events for subscriptions
            
            var userDB              = new FileDatabase("user_db", c.userDbPath, new UserDBHandler(), null, false);
            hub.Authenticator       = new UserAuthenticator(userDB) // optional - otherwise all request tasks are authorized
                .SubscribeUserDbChanges(hub.EventDispatcher);       // optional - apply user_db changes instantaneously
            hub.AddExtensionDB(userDB);                             // optional - expose userStore as extension database
            
            var httpHost            = new HttpHost(hub, "/fliox/").CacheControl(c.cache);
            httpHost.AddHandler      (new GraphQLHandler());
            httpHost.AddHandler      (new StaticFileHandler(c.www).CacheControl(c.cache)); // optional - serve static web files of Hub Explorer
            return httpHost;
        }
        
        private class Config {
            internal readonly string  dbPath              = "./DB~/main_db";
            internal readonly string  userDbPath          = "./DB~/user_db";
            internal readonly string  www                 = "../Json/Fliox.Hub.Explorer/www~"; // HubExplorer.Path;
            internal readonly string  cache               = null; // "max-age=600"; // HTTP Cache-Control
            internal readonly bool    useMemoryDbClone    = true;
        }
        
        private static EntityDatabase CreateDatabase(Config c, DatabaseSchema schema, TaskHandler handler) {
            var fileDb = new FileDatabase("main_db", c.dbPath, handler);
            fileDb.Schema = schema;
            if (!c.useMemoryDbClone)
                return fileDb;
            // As the DemoHub is also deployed as a demo service in the internet it uses a memory database
            // to minimize operation cost and prevent abuse as a free persistent database.   
            var memoryDB = new MemoryDatabase("main_db", handler);
            memoryDB.Schema = schema;
            memoryDB.SeedDatabase(fileDb).Wait();
            return memoryDB;
        }
    }
}
