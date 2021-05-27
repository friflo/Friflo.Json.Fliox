using System;
using System.CommandLine;
using System.CommandLine.Collections;
using System.CommandLine.Invocation;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Remote;
using Friflo.Json.Tests.Common.Utils;

namespace Friflo.Json.Tests
{
    // [Creating Modern And Helpful Command Line Utilities With System.CommandLine - .NET Core Tutorials]
    // https://dotnetcoretutorials.com/2021/01/16/creating-modern-and-helpful-command-line-utilities-with-system-commandline/
    static class Program
    {
        enum Module
        {
            GraphServer
        }
        
        // run examples:
        // dotnet Friflo.Json.Tests.dll --module GraphServer
        // dotnet run --project .\Json.Tests\Friflo.Json.Tests.csproj -- --module GraphServer
        public static void Main(string[] args)
        {
            // Console.WriteLine("Friflo.Json.Tests");
            var modules = new SymbolSet();
            var moduleOpt = new Option<Module>("--module",  "the module inside Friflo.Json.Tests") {IsRequired = true};

            var rootCommand = new RootCommand {
                moduleOpt, 
                new Option<string>("--database", () => "assets/db",  "folder of the file database")
            };
            rootCommand.Description = "small tests within Friflo.Json.Tests";

            rootCommand.Handler = CommandHandler.Create<Module, string>((module, database) =>
            {
                Console.WriteLine($"module: {module}");
                switch (module) {
                    case Module.GraphServer:    GraphHttp(database);                                break;
                    default:                    Console.WriteLine($"unknown module: {module}");    break;
                }
            });
            rootCommand.Invoke(args);
        }
        
        private static void GraphHttp(string database) {
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + database))
            using (var hostDatabase = new HttpHostDatabase(fileDatabase, "http://+:8080/")) {
                hostDatabase.Start();
                hostDatabase.Run();
            }
        }

    }
}