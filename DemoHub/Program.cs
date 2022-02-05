using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.DemoHub
{
    internal  static class  Program
    {
        public static void Main(string[] args) {
            FlioxServer("http://+:8010/");
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
        private static void FlioxServer(string endpoint) {
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
        ///   <item> By <b>HttpListener</b> see <see cref="FlioxServer"/> </item>
        ///   <item> By <b>ASP.NET Core / Kestrel</b> see <see cref="Startup.Configure"/></item>
        /// </list>
        /// <br/>
        /// The features of a <see cref="HttpHostHub"/> instance are:
        /// <list type="bullet">
        ///   <item> Providing all common database operations to query, read, create, update, delete and patch records </item>
        ///   <item> Support of various database types: memory, file system, remote and NoSQL aka key-value or document databases </item>
        ///   <item> Expose access to the service in two ways:<br/>
        ///     1. POST via a single path ./ enabling batching multiple tasks in a single request <br/>
        ///     2. Common REST API to POST, GET, PUT, DELETE and PATCH with via a path like ./rest/database/container/id <br/>
        ///   </item>
        ///   <item> Enable Messaging and Pub-Sub to send messages or commands and setup subscriptions by multiple clients </item>
        ///   <item> Enable user authentication and authorization of tasks requested by a user </item>
        ///   <item> Access and change user permission and roles required for authorization via the extension database: user_db </item>
        ///   <item> Expose server Monitoring as an extension database to get statistics about requests and tasks executed by users and clients </item>
        ///   <item> Adding a database schema to: <br/>
        ///     1. validate records written to the database by its schema definition <br/>
        ///     2. create type definitions for various languages: Typescript, C#, Kotlin, JSON Schema and Html <br/>
        ///     3. display entities as table in Hub Explorer <br/>
        ///     4. enable JSON auto completion, validation and reference links in Hub Explorer editor <br/>
        ///   </item>
        ///   <item> Add the Hub Explorer to: <br/>
        ///     1. browse databases, containers and entities <br/>
        ///     2. execute container queries using a LINQ filter expression <br/>
        ///     3. execute standard or custom database commands. E.g. DbEcho <br/>
        ///     4. send batch requests via HTTP or WebSocket to the Fliox.Hub server using the Playground <br/>
        ///   </item>
        /// </list>
        ///  Note: All extension databases added by <see cref="FlioxHub.AddExtensionDB"/> could be exposed by an
        /// additional <see cref="HttpHostHub"/> only accessible from Intranet as they contains sensitive data.
        /// </summary>
        private static HttpHostHub CreateHttpHost() {
            var database            = new FileDatabase(DbPath, new DemoHandler(), null, false);
            var hub                 = new FlioxHub(database).SetInfo("Demo Hub", "https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json.Tests/Main/Program.cs");
            hub.AddExtensionDB (ClusterDB.Name, new ClusterDB(hub));    // optional - expose info about catalogs (databases) as extension database
            hub.AddExtensionDB (MonitorDB.Name, new MonitorDB(hub));    // optional - expose monitor stats as extension database
            hub.EventBroker         = new EventBroker(true);            // optional - eventBroker enables Instant Messaging & Pub-Sub
            
            var userDB              = new FileDatabase(UserDbPath, new UserDBHandler(), null, false);
            hub.Authenticator       = new UserAuthenticator(userDB);    // optional - otherwise all request tasks are authorized
            hub.AddExtensionDB("user_db", userDB);                      // optional - expose userStore as extension database
            
            var typeSchema          = new NativeTypeSchema(typeof(DemoStore)); // optional - create TypeSchema from Type 
            database.Schema         = new DatabaseSchema(typeSchema);   // optional - enables type validation for create, upsert & patch operations
            var hostHub             = new HttpHostHub(hub).CacheControl(Cache);
            hostHub.AddHandler       (new StaticFileHandler(Www).CacheControl(Cache)); // optional - serve static web files of Hub Explorer
            return hostHub;
        }
        
        private static readonly string  DbPath      = "./DB/DemoStore";
        private static readonly string  UserDbPath  = "./DB/UserStore";
        private static readonly string  Www         = HubExplorer.Path;
        private static readonly string  Cache       = null; // "max-age=600"; // HTTP Cache-Control
        
        private static HttpHostHub CreateMiniHost() {
            // Run a minimal Fliox server without monitoring, messaging, Pub-Sub, user authentication / authorization & entity validation
            var database            = new FileDatabase(DbPath);
            var hub          	    = new FlioxHub(database);
            var hostHub             = new HttpHostHub(hub);
            hostHub.AddHandler       (new StaticFileHandler(Www, Cache));   // optional - serve static web files of Hub Explorer
            return hostHub;
        }
    }
}
