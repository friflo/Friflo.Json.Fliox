using System;
using System.Threading.Tasks;
using Demo;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.GraphQL;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;

namespace DemoHub
{
    /// <summary>Bootstrapping of databases hosted by <see cref="HttpHost"/></summary> 
    internal  static class  Program
    {
        /// <summary>
        /// This method is a blueprint showing how to setup a <see cref="HttpHost"/> utilizing all features available
        /// via HTTP and WebSockets. The Hub can be integrated by two different HTTP servers:
        /// <list type="bullet">
        ///   <item> By <see cref="System.Net.HttpListener"/> see <see cref="HttpServer.RunHost"/> </item>
        ///   <item> By <a href="https://docs.microsoft.com/en-us/aspnet/core/">ASP.NET Core 6.0 / Kestrel</a> see <see cref="StartupAsp6"/></item>
        /// </list>
        /// The features of a <see cref="HttpHost"/> instance utilized by this blueprint method are listed at
        /// <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Fliox.Hub/Host/README.md#httphost">Host README.md</a><br/>
        /// <i>Note</i>: All extension databases added by <see cref="FlioxHub.AddExtensionDB"/> could be exposed by an
        /// additional <see cref="HttpHost"/> instance only accessible from Intranet as they contains sensitive data.
        /// </summary>
        public static async Task Main(string[] args)
        {
            var schema      = DatabaseSchema.Create<DemoClient>(); // optional - create TypeSchema from Type
            var database    = CreateDatabase("memory", schema).AddCommands(new DemoCommands());
            var hub         = new FlioxHub(database);
            hub.Info.Set ("DemoHub", "dev", "https://github.com/friflo/Fliox.Examples#demo", "rgb(0 171 145)"); // optional
            hub.UseClusterDB(); // optional - expose info of hosted databases. cluster is required by HubExplorer
            hub.UseMonitorDB(); // optional - expose monitor stats as extension database
            hub.UsePubSub();    // optional - enables Pub-Sub (sending events for subscriptions)
            var userDB = new FileDatabase("user_db", "../Test/DB/user_db", UserDB.Schema, new UserDBService()) { Pretty = false };
            await hub.UseUserDB(userDB);
            // --- create HttpHost
            var httpHost = new HttpHost(hub, "/fliox/");
            httpHost.UseGraphQL();
            httpHost.UseStaticFiles(HubExplorer.Path); // optional - HubExplorer nuget: https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.Explorer
            httpHost.UseStaticFiles("www");            // optional - add www/example requests
            // await CreateWebRtcServer(httpHost);
            
            var server = HttpHost.GetArg(args, "--server");
            switch (server) {
                case null:
                case "HttpListener":    HttpServer.RunHost("http://+:8010/", httpHost); return; // HttpListener from BCL
                case "asp.net3":        StartupAsp3.Run(args, httpHost);                return; // ASP.NET Core 3, 3.1, 5
                case "asp.net6":        StartupAsp6.Run(args, httpHost);                return; // ASP.NET Core 6
            }
        }
        
        /* private static async Task CreateWebRtcServer(HttpHost httpHost) {
            var rtcConfig = new SignalingConfig {
                SignalingHost   = "ws://localhost:8011/fliox/",
                User            = "admin", Token = "admin",
                WebRtcConfig    = new WebRtcConfig { IceServerUrls = new [] { "stun:stun.sipsorcery.com" } },
            };
            var rtcServer = new RtcServer(rtcConfig);
            await rtcServer.AddHost("abc", httpHost);
        } */
        
        private static EntityDatabase CreateDatabase(string provider, DatabaseSchema schema)
        {
            var fileDb = new FileDatabase("main_db", "../Test/DB/main_db", schema);
            switch (provider) {
                case "file":    return fileDb;
                case "memory":  return new MemoryDatabase("main_db", schema).SeedDatabase(fileDb).Result;
            }
            throw new InvalidOperationException($"unknown provider: {provider}"); 
        }
    }
}
