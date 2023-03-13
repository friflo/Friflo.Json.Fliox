#if NET6_0_OR_GREATER

using Friflo.Json.Fliox.Hub.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Tests.Main
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            var hostHub = Program.CreateHttpHost(new Program.Config());
            hostHub.sharedEnv.Logger = new HubLoggerAspNetCore(loggerFactory);

            app.UseRouting();
            app.UseWebSockets();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("hello/", async context => {
                    await context.Response.WriteAsync("Hello World!");
                });
                endpoints.Map("/", async context => {
                    context.Response.Redirect(hostHub.baseRoute, false);
                    await context.Response.WriteAsync("redirect");
                });
                endpoints.Map("/fliox/{*path}", async context => {
                    var response = await context.ExecuteFlioxRequest(hostHub).ConfigureAwait(false);
                    // response can be logged and additional http headers can be added here
                    await context.WriteFlioxResponse(response).ConfigureAwait(false);
                });
            });
        }
    }
}

#endif