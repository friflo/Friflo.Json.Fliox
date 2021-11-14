using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Host.Monitor;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.UserAuth;
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
            //var hostHub = CreateMinimalHost("./Json.Tests/assets~/DB/PocStore", "./Json.Tests/www");
            var server = new HttpListenerHost(endpoint, hostHub);
            server.Start();
            server.Run();
        }
        
        /// <summary>
        /// This method is intended to be a blueprint of a <see cref="HttpHostHub"/> utilizing all features available
        /// via HTTP and WebSockets. These features are:
        /// <list type="bullet">
        ///   <item> Providing all common database table operations to query, read, create, updates and delete records </item>
        ///   <item> Enabling Messaging and Pub-Sub to send messages or commands and setup subscriptions by multiple clients </item>
        ///   <item> Enabling user authentication and authorization of tasks requested by a user </item>
        ///   <item> Access and change user permission and roles required for authorization via the extension database: user_db</item>
        ///   <item> Expose the server Monitor as an extension database to get statistics about requests and tasks executed by users and clients </item>
        ///   <item> Adding a database schema to validate records written to the default database and exposing it as JSON Schema </item>
        ///   <item> Adding a web playground to send and validate arbitrary requests to the Fliox server </item>
        /// </list>
        ///  Note: Both extension databases added by <see cref="FlioxHub.AddExtensionDB"/> could be exposed by an
        /// additional <see cref="HttpHostHub"/> only accessible from Intranet as they contains sensitive data.
        /// </summary>         
        public static HttpHostHub CreateHttpHost(string dbPath, string userDbPath, string wwwPath) {
            var database            = new FileDatabase(dbPath);
            var hub                 = new FlioxHub(database);
            hub.AddExtensionDB (MonitorDB.Name, new MonitorDB(hub));    // optional - expose monitor stats as extension database
            hub.EventBroker         = new EventBroker(true);            // optional - eventBroker enables Instant Messaging & Pub-Sub
            
            var userDB              = new FileDatabase(userDbPath, new UserDBHandler());
            hub.Authenticator       = new UserAuthenticator(userDB);    // optional - otherwise all request tasks are authorized
            hub.AddExtensionDB("user_db", userDB);                      // optional - expose userStore as extension database
            
            var typeSchema          = new NativeTypeSchema(typeof(PocStore)); // optional - used by DatabaseSchema & SchemaHandler
        //  var typeSchema          = CreateTypeSchema();               // alternatively create typeSchema from JSON Schema 
            database.Schema         = new DatabaseSchema(typeSchema);   // optional - enables type validation for create, upsert & patch operations
            var hostHub             = new HttpHostHub(hub);
            hostHub.requestHandler  = new RequestHandler(wwwPath);      // optional - used to serve static web content
            hostHub.schemaHandler   = new SchemaHandler("/schema/", typeSchema, Utils.Zip); // optional - Web UI to serve DB schema as files (JSON Schema, ...)
            return hostHub;
        }
        
        private static HttpHostHub CreateMinimalHost(string dbPath, string wwwPath) {
            // Run a minimal Fliox server without monitoring, messaging, Pub-Sub, user authentication / authorization & entity validation
            var database            = new FileDatabase(dbPath);
            var hub          	    = new FlioxHub(database);
            var hostHub             = new HttpHostHub(hub);
            hostHub.requestHandler  = new RequestHandler(wwwPath);   // optional. Used to serve static web content
            return hostHub;
        }
        
        private static TypeSchema CreateTypeSchema() {
            var schemas = JsonTypeSchema.ReadSchemas("./Json.Tests/assets~/Schema/JSON/PocStore");
            return new JsonTypeSchema(schemas, "./UnitTest.Fliox.Client.json#/definitions/PocStore");
        }
    }
}
