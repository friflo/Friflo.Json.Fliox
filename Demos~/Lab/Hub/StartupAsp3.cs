using System;
using Friflo.Json.Fliox.Hub.AspNetCore;
using Friflo.Json.Fliox.Hub.Remote;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LabHub
{
    /// <summary>Bootstrapping of ASP.NET Core 3, 3.1, 5 and adding a <see cref="HttpHost"/>.</summary> 
    public class StartupAsp3
    {
        internal static void Run(string[] args, HttpHost httpHost)
        {
            CreateHostBuilder(args, httpHost).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args, HttpHost httpHost)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                        webBuilder
                            .ConfigureServices(services => services.AddSingleton(httpHost))
                            .UseStartup<StartupAsp3>()
                            // .UseKestrel(options => {options.Listen(IPAddress.Loopback, 5000); }) // use http instead of https
                            .UseKestrel()
                            .UseUrls("http://localhost:5000") // required for Docker
                ).ConfigureLogging(logging => {
                    // single line logs
                    logging.ClearProviders();
                    logging.AddConsole(configure => { configure.FormatterName = "Systemd"; }); 
                });
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, HttpHost httpHost)
        {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            Console.WriteLine($"Hub Explorer - { httpHost.GetStartPage(app.ApplicationServices)}\n");
            httpHost.UseAspNetCoreLogger(app.ApplicationServices);

            app.UseRouting();
            app.UseWebSockets();

            app.UseEndpoints(endpoints => {
                endpoints.MapGet("hello/", () => "Hello World");
                // add redirect only to enable using http://localhost:5000 for debugging
                endpoints.MapRedirect("/",          httpHost);
                endpoints.MapHost("/fliox/{*path}", httpHost);
            });
        }
    }
}
