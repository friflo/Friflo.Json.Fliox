using Friflo.Json.Fliox.DB.AspNetCore;
using Friflo.Json.Fliox.DB.Host;
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
            var database        = new MemoryDatabase();
            var hostDatabase    = new AspNetCoreHostHostDatabase (database);

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGet("/", async context =>
				{
					await context.Response.WriteAsync("Hello World!");
				});
                
                endpoints.MapGet("fliox/", async context => {
                    await hostDatabase.ExecuteGet(context);
                });
                endpoints.MapPost("fliox/", async context => {
                    await hostDatabase.ExecutePost(context);
                });
            });
		}
	}
}
