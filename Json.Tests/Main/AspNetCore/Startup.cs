#if !UNITY_2020_1_OR_NEWER

using Friflo.Json.Fliox.Hub.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            var hostHub = Program.CreateHttpHost();

            app.UseRouting();
            app.UseWebSockets();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("hello/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
                
                endpoints.Map("/{*path}", async context => {
                    var response = await context.HandleFlioxHostRequest(hostHub).ConfigureAwait(false);
                    // response can be logged and additional http headers can be added here
                    await context.HandleFlioxHostResponse(response).ConfigureAwait(false);
                });
            });
        }
    }
}

#endif