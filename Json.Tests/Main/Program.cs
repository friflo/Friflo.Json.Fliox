using System;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Event;
using Friflo.Json.Fliox.DB.Host.Monitor;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.DB.UserAuth;
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
        private static void FlioxServerMinimal(string endpoint, string databaseFolder) {
            // Run a minimal Fliox server without monitoring, Pub-Sub, user authentication / authorization, entity validation
            Console.WriteLine($"FileDatabase: {databaseFolder}");
            var database                = new FileDatabase(databaseFolder);
            var host                    = new HttpListenerHost(endpoint, database);
            host.database.requestHandler= new RequestHandler("./Json.Tests/www");   // optional. Used to serve static web content
            host.Start();
            host.Run();
        }
        
        private static void FlioxServer(string endpoint, string databaseFolder) {
            Console.WriteLine($"FileDatabase: {databaseFolder}");
            var database                = new FileDatabase(databaseFolder);
            
            database.AddExtensionDB      (new MonitorDatabase(database));           // optional. enables monitoring database access
            database.EventBroker        = new EventBroker(true);                    // optional. eventBroker enables Pub-Sub
            database.Authenticator      = CreateUserAuthenticator();                // optional. Otherwise all request tasks are authorized
            
            var typeSchema              = CreateTypeSchema(true);                   // optional. used by DatabaseSchema & SchemaHandler
            database.Schema             = new DatabaseSchema(typeSchema);           // optional. Enables type validation for create, upsert & patch operations
            
            var host                    = new HttpListenerHost(endpoint, database);
            host.database.requestHandler= new RequestHandler("./Json.Tests/www");   // optional. Used to serve static web content
            host.database.schemaHandler = new SchemaHandler("/schema/", typeSchema, Utils.Zip); // optional. Web UI for database schema
            host.Start();
            host.Run();
        }
        
        private static TypeSchema CreateTypeSchema(bool fromJsonSchema) {
            if (fromJsonSchema) {
                var schemas = JsonTypeSchema.ReadSchemas("./Json.Tests/assets~/Schema/JSON/PocStore");
                return new JsonTypeSchema(schemas, "./UnitTest.Fliox.Client.json#/definitions/PocStore");
            }
            // using a NativeTypeSchema add an additional dependency by using the EntityStore: PocStore
            return new NativeTypeSchema(typeof(PocStore));
        }
        
        private static UserAuthenticator CreateUserAuthenticator () {
            var userDatabase    = new FileDatabase("./Json.Tests/assets~/DB/UserStore");
            var userStore       = new UserStore (userDatabase, UserStore.AuthenticationUser, null);
            var _               = new UserDatabaseHandler   (userDatabase);
            return new UserAuthenticator(userStore, userStore);
        }
    }
}
