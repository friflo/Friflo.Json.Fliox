using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;

// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Tests.Main
{
    internal  static partial class  Program
    {
        // Example requests for server at: /Json.Tests/www/example-requests/
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
            var hostHub = CreateHttpHost("./Json.Tests/assets~/DB/PocStore", "./Json.Tests/assets~/DB/UserStore", "./Json.Tests/www");
        //  var hostHub = CreateMiniHost("./Json.Tests/assets~/DB/PocStore", "./Json.Tests/www");
            var server = new HttpListenerHost(endpoint, hostHub);
            server.Start();
            server.Run();
        }
        
        /// <summary>
        /// This method is a blueprint showing how to setup a <see cref="HttpHostHub"/> utilizing all features available
        /// via HTTP and WebSockets. The Hub can be integrated by two different HTTP servers:
        /// <list type="bullet">
        ///   <item> By <b>HttpListener</b> see <see cref="FlioxServer"/> </item>
        ///   <item> By <b>Kestrel</b> see <see cref="Startup.Configure"/></item>
        /// </list>
        /// <br/>
        /// The features of a <see cref="HttpHostHub"/> instance are:
        /// <list type="bullet">
        ///   <item> Providing all common database operations to query, read, create, updates and delete records</item>
        ///   <item> Expose access to the service in two ways:<br/>
        ///     1. POST via a single path ./ enabling batching multiple tasks in a single request<br/>
        ///     2. Common REST API to POST, GET, PUT, DELETE and PATCH with via a path like ./rest/database/container/id
        ///   </item>
        ///   <item> Enable Messaging and Pub-Sub to send messages or commands and setup subscriptions by multiple clients </item>
        ///   <item> Enable user authentication and authorization of tasks requested by a user </item>
        ///   <item> Access and change user permission and roles required for authorization via the extension database: user_db</item>
        ///   <item> Expose server Monitoring as an extension database to get statistics about requests and tasks executed by users and clients </item>
        ///   <item> Adding a database schema to:
        ///     1. validate records written to the database by its schema definition.
        ///     2. create type definitions for various languages: Typescript, C#, Kotlin, JSON Schema & Html.      
        ///   </item>
        ///   <item> Adding a web App to with database Explorer and Playground to send and validate arbitrary requests to the Fliox.Hub server </item>
        /// </list>
        ///  Note: All extension databases added by <see cref="FlioxHub.AddExtensionDB"/> could be exposed by an
        /// additional <see cref="HttpHostHub"/> only accessible from Intranet as they contains sensitive data.
        /// </summary>         
        public static HttpHostHub CreateHttpHost(string dbPath, string userDbPath, string wwwPath) {
            var database            = new FileDatabase(dbPath, new PocHandler(), null, false);
            var hub                 = new FlioxHub(database);
            hub.AddExtensionDB (ClusterDB.Name, new ClusterDB(hub));    // optional - expose info about catalogs (databases) as extension database
            hub.AddExtensionDB (MonitorDB.Name, new MonitorDB(hub));    // optional - expose monitor stats as extension database
            hub.EventBroker         = new EventBroker(true);            // optional - eventBroker enables Instant Messaging & Pub-Sub
            
            var userDB              = new FileDatabase(userDbPath, new UserDBHandler(), null, false);
            hub.Authenticator       = new UserAuthenticator(userDB);    // optional - otherwise all request tasks are authorized
            hub.AddExtensionDB("user_db", userDB);                      // optional - expose userStore as extension database
            
            var typeSchema          = new NativeTypeSchema(typeof(PocStore)); // optional - used by DatabaseSchema & SchemaHandler
        //  var typeSchema          = CreateTypeSchema();               // alternatively create typeSchema from JSON Schema 
            database.Schema         = new DatabaseSchema(typeSchema);   // optional - enables type validation for create, upsert & patch operations
            var hostHub             = new HttpHostHub(hub);
            hostHub.AddHandler       (new RequestHandler(wwwPath));     // optional - used to serve static web content
            return hostHub;
        }
        
        private static HttpHostHub CreateMiniHost(string dbPath, string wwwPath) {
            // Run a minimal Fliox server without monitoring, messaging, Pub-Sub, user authentication / authorization & entity validation
            var database            = new FileDatabase(dbPath);
            var hub          	    = new FlioxHub(database);
            var hostHub             = new HttpHostHub(hub);
            hostHub.AddHandler       (new RequestHandler(wwwPath));     // optional. Used to serve static web content
            return hostHub;
        }
        
        private static TypeSchema CreateTypeSchema() {
            var schemas = JsonTypeSchema.ReadSchemas("./Json.Tests/assets~/Schema/JSON/PocStore");
            return new JsonTypeSchema(schemas, "./UnitTest.Fliox.Client.json#/definitions/PocStore");
        }
    }
}
