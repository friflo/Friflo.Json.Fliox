#if !UNITY_2020_1_OR_NEWER

using System;
using System.CommandLine;
using System.CommandLine.Collections;
using System.CommandLine.Invocation;
using System.IO;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Remote;

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
        
        // run GraphServer via one of the following methods:
        // > dotnet ./Json.Tests/.bin/Debug/netcoreapp3.1/Friflo.Json.Tests.dll --module GraphServer
        // > dotnet run --project ./Json.Tests/Friflo.Json.Tests.csproj -- --module GraphServer        (also compiles project)
        // VSCode        > Run > GraphServer
        // Rider         > Run > GraphServer
        // Visual Studio > Debug > GraphServer
        public static void Main(string[] args)
        {
            Console.WriteLine($"Friflo.Json.Tests directory: {Directory.GetCurrentDirectory()}");
            var modules = new SymbolSet();
            var moduleOpt = new Option<Module>("--module",  "the module inside Friflo.Json.Tests") {IsRequired = true};

            var rootCommand = new RootCommand {
                moduleOpt, 
                new Option<string>("--database", () => "./Json.Tests/assets/db",  "folder of the file database")
            };
            rootCommand.Description = "small tests within Friflo.Json.Tests";

            rootCommand.Handler = CommandHandler.Create<Module, string>((module, database) =>
            {
                Console.WriteLine($"module: {module}");
                switch (module) {
                    case Module.GraphServer:    GraphServer(database);                             break;
                    default:                    Console.WriteLine($"unknown module: {module}");    break;
                }
            });
            rootCommand.Invoke(args);
        }
        
        // Http server requires setting permission to run an http server.
        // Otherwise exception is thrown on startup: System.Net.HttpListenerException: permission denied.
        // To give access see: [add urlacl - Win32 apps | Microsoft Docs] https://docs.microsoft.com/en-us/windows/win32/http/add-urlacl
        //     netsh http add urlacl url=http://+:8081/ user=<DOMAIN>\<USER> listen=yes
        //     netsh http delete urlacl http://+:8081/
        // 
        // Get DOMAIN\USER via  PowerShell
        //     $env:UserName
        //     $env:UserDomain 
        private static void GraphServer(string database) {
            using (var fileDatabase = new FileDatabase(database))
            using (var hostDatabase = new HttpHostDatabase(fileDatabase, "http://+:8081/")) {
                hostDatabase.Start();
                hostDatabase.Run();
            }
        }

    }
}

#endif
