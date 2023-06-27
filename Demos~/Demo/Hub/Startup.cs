using Friflo.Json.Fliox.Hub.AspNetCore;
using Friflo.Json.Fliox.Hub.Remote;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace DemoHub;

/// <summary>Bootstrapping of ASP.NET Core 6.0 and adding a <see cref="HttpHost"/> </summary> 
public static class Startup
{
    public static void Run(string[] args, HttpHost httpHost)
    {
        var builder     = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8010));
        var app         = builder.Build();

        httpHost.UseAspNetCoreLogger(app.Services);
        
        app.UseWebSockets();                        // required for Pub-SUb

        app.MapGet("hello/", () => "Hello World");
          
        app.MapRedirect("/", httpHost);             // optional: add redirect to Hub Explorer at http://localhost:8010
        app.MapHost("/fliox/{*path}", httpHost);    // ASP.NET Core 6.0 integration
        
        app.RunLogUrl(httpHost);                    // same as app.Run(); and logging of base URL
    }
}