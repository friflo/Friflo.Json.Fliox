#if !UNITY_2020_1_OR_NEWER

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.DB.NoSQL.Event;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Graph;

namespace Friflo.Json.Tests.Main
{
    static class Program
    {
        enum Module
        {
            GraphServer,
            //
            MemoryDbThroughput,
            FileDbThroughput,
            WebsocketDbThroughput,
            HttpDbThroughput,
            LoopbackDbThroughput
        }
        
        // run GraphServer via one of the following methods:
        //   dotnet run --project ./Json.Tests/Friflo.Json.Tests.csproj -- --module GraphServer        (also compiles project)
        //   dotnet ./Json.Tests/.bin/Debug/netcoreapp3.1/Friflo.Json.Tests.dll --module GraphServer   (requires Debug build)
        //   VSCode        > Run > GraphServer
        //   Rider         > Run > GraphServer
        //   Visual Studio > Debug > GraphServer
        public static void Main(string[] args)
        {
            Console.WriteLine($"Friflo.Json.Tests - current directory: {Directory.GetCurrentDirectory()}");

            // [Creating Modern And Helpful Command Line Utilities With System.CommandLine - .NET Core Tutorials]
            // https://dotnetcoretutorials.com/2021/01/16/creating-modern-and-helpful-command-line-utilities-with-system-commandline/
            var moduleOpt = new Option<Module>("--module",  "the module inside Friflo.Json.Tests") {IsRequired = true};

            var rootCommand = new RootCommand {
                moduleOpt,
                new Option<string>("--endpoint", () => "http://+:8010/",                        "endpoint the server listen at"),
                new Option<string>("--database", () => "./Json.Tests/assets~/DB/PocStore",    "folder of the file database"),
                new Option<string>("--www",      () => "./Json.Tests/assets~/www",               "folder of static web files")
            };
            rootCommand.Description = "small tests within Friflo.Json.Tests";

            rootCommand.Handler = CommandHandler.Create<Module, string, string, string>(async (module, endpoint, database, www) =>
            {
                Console.WriteLine($"module: {module}");
                switch (module) {
                    case Module.GraphServer:
                        GraphServer(endpoint, database, www);
                        break;
                    case Module.MemoryDbThroughput:
                        await Throughput.MemoryDbThroughput();
                        break;
                    case Module.FileDbThroughput:
                        await Throughput.FileDbThroughput();
                        break;
                    case Module.WebsocketDbThroughput:
                        await Throughput.WebsocketDbThroughput();
                        break;
                    case Module.HttpDbThroughput:
                        await Throughput.HttpDbThroughput();
                        break;
                    case Module.LoopbackDbThroughput:
                        await Throughput.LoopbackDbThroughput();
                        break;
                }
            });
            rootCommand.Invoke(args);
        }
        
        // Example requests to the GraphServer are at: ./GraphServer.http
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
        private static void GraphServer(string endpoint, string database, string wwwRoot) {
            Console.WriteLine($"FileDatabase: {database}");
            var fileDatabase        = new FileDatabase(database) { eventBroker = new EventBroker(true) }; // eventBroker enables Pub-Sub
            
            // adding DatabaseSchema is optional - it enables type validation for create, upsert & patch operations
            var typeSchema          = GetTypeSchema(true);
            fileDatabase.schema     = new DatabaseSchema(typeSchema);
            
            var contextHandler      = new HttpContextHandler(wwwRoot);
            var hostDatabase        = new HttpHostDatabase(fileDatabase, endpoint, contextHandler);
            hostDatabase.schemaHandler = new SchemaHandler(Utils.Zip); // optional - generate zip archives for schemas
            hostDatabase.Start();
            hostDatabase.Run();
        }
        
        private static TypeSchema GetTypeSchema(bool fromJsonSchema) {
            if (fromJsonSchema) {
                var schemas = JsonTypeSchema.ReadSchemas("./Json.Tests/assets~/Schema/JSON/PocStore");
                return new JsonTypeSchema(schemas, "./UnitTest.Fliox.Graph.json#/definitions/PocStore");
            }
            // using a NativeTypeSchema add an additional dependency by using the EntityStore: PocStore
            return new NativeTypeSchema(typeof(PocStore));
        }
    }
}

#endif
