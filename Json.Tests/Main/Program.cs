#if !UNITY_2020_1_OR_NEWER

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Database.Remote;
using Friflo.Json.Tests.Common.UnitTest.Flow.Graph;
using Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy;

namespace Friflo.Json.Tests.Main
{
    static class Program
    {
        enum Module
        {
            GraphServer,
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
                new Option<string>("--endpoint", () => "http://+:8081/",                "endpoint the server listen at"),
                new Option<string>("--database", () => "./Json.Tests/assets/Graph/PocStore",  "folder of the file database"),
                new Option<string>("--www",      () => "./Json.Tests/assets/www",       "folder of static web files")
            };
            rootCommand.Description = "small tests within Friflo.Json.Tests";

            rootCommand.Handler = CommandHandler.Create<Module, string, string, string>(async (module, endpoint, database, www) =>
            {
                Console.WriteLine($"module: {module}");
                switch (module) {
                    case Module.GraphServer:
                        GraphServer(endpoint, database, www, false);
                        break;
                    case Module.MemoryDbThroughput:
                        await MemoryDbThroughput();
                        break;
                    case Module.FileDbThroughput:
                        await FileDbThroughput();
                        break;
                    case Module.WebsocketDbThroughput:
                        await WebsocketDbThroughput();
                        break;
                    case Module.HttpDbThroughput:
                        await HttpDbThroughput();
                        break;
                    case Module.LoopbackDbThroughput:
                        await LoopbackDbThroughput();
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
        //     netsh http add urlacl url=http://+:8081/ user=<DOMAIN>\<USER> listen=yes
        //     netsh http delete urlacl http://+:8081/
        // 
        // Get DOMAIN\USER via  PowerShell
        //     $env:UserName
        //     $env:UserDomain 
        private static void GraphServer(string endpoint, string database, string wwwRoot, bool simulateErrors) {
            Console.WriteLine($"FileDatabase: {database}");
            var fileDatabase = new FileDatabase(database) { eventBroker = new EventBroker(true) };
            EntityDatabase localDatabase = fileDatabase;
            if (simulateErrors) {
                var testDatabase = new TestDatabase(fileDatabase);
                // TestStoreErrors.AddSimulationErrors(testDatabase);
                localDatabase = testDatabase;
            }
            var contextHandler = new HttpContextHandler(wwwRoot);
            var hostDatabase = new HttpHostDatabase(localDatabase, endpoint, contextHandler);
            hostDatabase.Start();
            hostDatabase.Run();
        }
        
        private static async Task MemoryDbThroughput() {
            var db = new MemoryDatabase();
            await TestStore.ConcurrentAccess(db, 4, 0, 1_000_000, false);
        }
        
        private static async Task FileDbThroughput() {
            var db = new FileDatabase("./Json.Tests/assets/Graph/testConcurrencyDb");
            await TestStore.ConcurrentAccess(db, 4, 0, 1_000_000, false);
        }
        
        private static async Task WebsocketDbThroughput() {
            var db = new MemoryDatabase();
            using (var hostDatabase     = new HttpHostDatabase(db, "http://+:8080/", null))
            using (var remoteDatabase   = new WebSocketClientDatabase("ws://localhost:8080/")) {
                await TestStore.RunRemoteHost(hostDatabase, async () => {
                    await remoteDatabase.Connect();
                    await TestStore.ConcurrentAccess(remoteDatabase, 4, 0, 1_000_000, false);
                });
            }
        }
        
        private static async Task HttpDbThroughput() {
            var db = new MemoryDatabase();
            using (var hostDatabase     = new HttpHostDatabase(db, "http://+:8080/", null))
            using (var remoteDatabase   = new HttpClientDatabase("ws://localhost:8080/")) {
                await TestStore.RunRemoteHost(hostDatabase, async () => {
                    await TestStore.ConcurrentAccess(remoteDatabase, 4, 0, 1_000_000, false);
                });
            }
        }
        
        private static async Task LoopbackDbThroughput() {
            var db = new MemoryDatabase();
            using (var loopbackDatabase     = new LoopbackDatabase(db)) {
                await TestStore.ConcurrentAccess(loopbackDatabase, 4, 0, 1_000_000, false);
            }
        }

    }
}

#endif
