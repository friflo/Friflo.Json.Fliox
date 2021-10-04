#if !UNITY_2020_1_OR_NEWER

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Friflo.Json.Tests.Main
{
    internal static partial class Program
    {
        private enum Module
        {
            FlioxServer,
            //
            MemoryDbThroughput,
            FileDbThroughput,
            WebsocketDbThroughput,
            HttpDbThroughput,
            LoopbackDbThroughput
        }
        
        // run FlioxServer via one of the following methods:
        //   dotnet run --project ./Json.Tests/Friflo.Json.Tests.csproj -- --module FlioxServer        (also compiles project)
        //   dotnet ./Json.Tests/.bin/Debug/netcoreapp3.1/Friflo.Json.Tests.dll --module FlioxServer   (requires Debug build)
        //   VSCode        > Run > FlioxServer
        //   Rider         > Run > FlioxServer
        //   Visual Studio > Debug > FlioxServer
        public static void Main(string[] args)
        {
            Console.WriteLine($"Friflo.Json.Tests - current directory: {Directory.GetCurrentDirectory()}");

            // [Creating Modern And Helpful Command Line Utilities With System.CommandLine - .NET Core Tutorials]
            // https://dotnetcoretutorials.com/2021/01/16/creating-modern-and-helpful-command-line-utilities-with-system-commandline/
            var moduleOpt = new Option<Module>("--module",  "the module inside Friflo.Json.Tests") {IsRequired = true};

            var rootCommand = new RootCommand {
                moduleOpt,
                new Option<string>("--endpoint", () => "http://+:8010/",                    "endpoint the server listen at"),
                new Option<string>("--database", () => "./Json.Tests/assets~/DB/PocStore",  "folder of the file database")
            };
            rootCommand.Description = "small tests within Friflo.Json.Tests";

            rootCommand.Handler = CommandHandler.Create<Module, string, string>(async (module, endpoint, database) =>
            {
                Console.WriteLine($"module: {module}");
                switch (module) {
                    case Module.FlioxServer:
                        FlioxServer(endpoint, database);
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
    }
}

#endif
