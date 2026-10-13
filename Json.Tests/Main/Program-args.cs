#if !UNITY_2020_1_OR_NEWER

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;

namespace Friflo.Json.Tests.Main
{
    internal static partial class Program
    {
        private enum Module
        {
            TestServer,
            TestDB,
            DropDatabase,
            FlioxServerAspNetCore,
            ListenEvents,
            //
            ReadThroughput,
            //
            MemoryDbThroughput,
            SQLiteThroughput,
            FileDbThroughput,
            WebsocketDbThroughput,
            UdpDbThroughput,
            HttpDbThroughput,
            LoopbackDbThroughput
        }
        
        // run TestServer via one of the following methods:
        //   dotnet run --project ./Json.Tests/Friflo.Json.Tests.csproj -- --module TestServer        (also compiles project)
        //   dotnet ./Json.Tests/.bin/Debug/netcoreapp3.1/Friflo.Json.Tests.dll --module TestServer   (requires Debug build)
        //   VSCode        > Run > TestServer
        //   Rider         > Run > TestServer
        //   Visual Studio > Debug > TestServer
        public static void Main(string[] args)
        {
            Console.WriteLine($"Friflo.Json.Tests - current directory: {Directory.GetCurrentDirectory()}");

            // [Creating Modern And Helpful Command Line Utilities With System.CommandLine - .NET Core Tutorials]
            // https://dotnetcoretutorials.com/2021/01/16/creating-modern-and-helpful-command-line-utilities-with-system-commandline/
            var moduleOpt = new Option<Module>("--module") {
                Required    = true,
                Description = "the module inside Friflo.Json.Tests"
            };

            var endpointOpt = new Option<string>("--endpoint") {
                DefaultValueFactory = result => "http://localhost:8011/",
                Description         = "endpoint the server listen at"
            };
            var clientOpt = new Option<string>("--client") {
                Description = "client id"
            };

            var rootCommand = new RootCommand {
                moduleOpt,
                endpointOpt,
                clientOpt
            };
            rootCommand.Description = "small tests within Friflo.Json.Tests";
            
            rootCommand.SetAction(async parseResult =>
            {
                var module   = parseResult.GetValue(moduleOpt);
                var endpoint = parseResult.GetValue<string>("--endpoint");
                var client   = parseResult.GetValue<string>("--client");
                
                client = string.IsNullOrEmpty(client) ? null : client;
                Console.WriteLine($"module: {module}");
                switch (module) {
                    case Module.TestServer:
                        TestServer(endpoint);
                        break;
                    case Module.TestDB:
                        await Provider.Program.Run();
                        break;
                    case Module.DropDatabase:
                        await Provider.Program.DropDatabases();
                        break;
#if NET6_0_OR_GREATER
                    case Module.FlioxServerAspNetCore:
                        FlioxServerAspNetCore(endpoint);
                        break;
#endif
                    case Module.ListenEvents:
                        await PocClient.ListenEvents(client);
                        break;
                    case Module.ReadThroughput:
                        await TestSyncPerf.ReadThroughput();
                        break;
                    case Module.MemoryDbThroughput:
                        await Throughput.MemoryDbThroughput();
                        break;
                    case Module.SQLiteThroughput:
                        await Throughput.SQLiteThroughput();
                        break;
                    case Module.FileDbThroughput:
                        await Throughput.FileDbThroughput();
                        break;
                    case Module.WebsocketDbThroughput:
                        await Throughput.WebsocketDbThroughput();
                        break;
                    case Module.UdpDbThroughput:
                        await Throughput.UdpDbThroughput();
                        break;
                    case Module.HttpDbThroughput:
                        await Throughput.HttpDbThroughput();
                        break;
                    case Module.LoopbackDbThroughput:
                        await Throughput.LoopbackDbThroughput();
                        break;
                }
            });
            rootCommand.Parse(args).Invoke();
        }
    }
}

#endif