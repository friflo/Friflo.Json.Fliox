using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.Mapper;
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
            var hostDatabase    = new HttpHostDatabase (database);

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGet("/", async context =>
				{
					await context.Response.WriteAsync("Hello World!");
				});
                
                endpoints.Map("fliox/", async context => {
                    var requestContent  = await JsonUtf8.ReadToEndAsync(context.Request.Body).ConfigureAwait(false);
                    var response        = await hostDatabase.ExecuteHttpRequest(context.Request.Method, requestContent);
                    var responseStream              = response.body.AsMemoryStream();
                    context.Response.Body           = responseStream;
                    context.Response.ContentLength  = responseStream.Length;
                    context.Response.StatusCode     = (int)response.status;
                });
            });
		}
	}
}
