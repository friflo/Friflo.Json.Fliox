using System;
using Friflo.Json.Fliox.Hub.AspNetCore;
using Friflo.Json.Fliox.Hub.Remote;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        
        app.UseWebSockets();

        app.MapGet("hello/", () => "Hello World");
        // add redirect only to enable using http://localhost:8010 for debugging  
        app.MapGet("/", async context => {
            context.Response.Redirect(httpHost.baseRoute, false);
            await context.Response.WriteAsync("redirect");
        });
        app.Map("/fliox/{*path}", async context =>  {
            var requestContext = await context.ExecuteFlioxRequest(httpHost).ConfigureAwait(false);
            await context.WriteFlioxResponse(requestContext).ConfigureAwait(false);
        });
        // use app.Start() / app.WaitForShutdown() instead of app.Run() to log start page
        app.Start();
        Console.WriteLine($"Hub Explorer - {httpHost.GetStartPage(app.Services)}\n");
        app.WaitForShutdown();
    }
}