using System;
using Friflo.Json.Fliox.Hub.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DemoHub;

/// <summary>
/// Bootstrapping of ASP.NET Core 6.0 and adding the Hub returned by <see cref="Program.CreateHttpHost"/>.
/// </summary> 
public static class StartupAsp6
{
    public static void Run(string[] args)
    {
        var httpHost    = Program.CreateHttpHost();
        
        var builder     = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8010));
        var app         = builder.Build();
        var server      = app.Services.GetRequiredService<IServer>();
        var addresses   = server.Features.Get<IServerAddressesFeature>().Addresses;
        var startPage   = httpHost.GetStartPage(addresses);
        Console.WriteLine($"Hub Explorer - {startPage}\n");
        
        var loggerFactory = app.Services.GetService<ILoggerFactory>();
        httpHost.sharedEnv.Logger = new HubLoggerAspNetCore(loggerFactory);
        
        app.UseWebSockets();

        app.MapGet("hello/", () => "Hello World");
        app.MapGet("/", async context => {
            context.Response.Redirect(httpHost.endpoint, false);
            await context.Response.WriteAsync("redirect");
        });

        app.Map("/fliox/{*path}", async context =>  {
            await context.HandleFlioxRequest(httpHost).ConfigureAwait(false);
        });
        app.Run();
    }
}