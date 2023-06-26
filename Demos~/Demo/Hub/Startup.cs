using System;
using Friflo.Json.Fliox.Hub.AspNetCore;
using Friflo.Json.Fliox.Hub.Remote;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace DemoHub;

/// <summary>
/// Bootstrapping of ASP.NET Core 6.0 and adding a <see cref="HttpHost"/>.
/// </summary> 
public static class Startup
{
    public static void Run(string[] args, HttpHost httpHost)
    {
        var builder     = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8010));
        var app         = builder.Build();

        httpHost.UseAspNetCoreLogger(app.Services);
        
        app.UseWebSockets();    // required for Pub-SUb

        app.MapGet("hello/", () => "Hello World");
          
        app.MapRedirect("/", httpHost); // optional: add redirect to Hub Explorer to use http://localhost:8010 for debugging
        app.MapHost("/fliox/{*path}", httpHost);
        
        // using app.Start() / app.WaitForShutdown() instead of app.Run() to log start page
        app.Start();
        Console.WriteLine($"Hub Explorer - {httpHost.GetStartPage(app.Services)}\n");
        app.WaitForShutdown();
    }
}