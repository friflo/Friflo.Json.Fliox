#if NET6_0_OR_GREATER

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Tests.Main
{
    internal static partial class Program
    {
        private static void FlioxServerAspNetCore(string endpoint)
        {
            string [] args = {};
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(loggingBuilder => loggingBuilder.AddFilter<ConsoleLoggerProvider>(level => level == LogLevel.None))
                .ConfigureWebHostDefaults(webBuilder => 
                    webBuilder.UseStartup<Startup>()
                        .UseKestrel(options => {options.Listen(IPAddress.Loopback, 8010); }) // use http instead of https
                );
        }
    }
}

#endif
