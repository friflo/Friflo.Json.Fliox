using System;
using System.Threading.Tasks;
using Lab;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.GraphQL;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.WebRTC;

namespace LabHub
{
    /// <summary>Bootstrapping of databases hosted by <see cref="HttpHost"/></summary> 
    internal  static class  Program
    {
        public static void Main(string[] args)
        {
            var schema      = DatabaseSchema.Create<LabClient>(); // create TypeSchema from Type
            var database    = CreateDatabase("memory", schema);
            var hub         = new FlioxHub(database);
            hub.Info.Set ("LabHub", "dev", "https://github.com/friflo/Fliox.Examples#demo", "rgb(0 171 145)"); // optional
            hub.UseClusterDB(); // optional - expose info of hosted databases. cluster is required by HubExplorer
            hub.UseMonitorDB(); // optional - expose monitor stats as extension database
            hub.UsePubSub();    // optional - enables Pub-Sub (sending events for subscriptions)
            // --- create HttpHost
            var httpHost = new HttpHost(hub, "/fliox/");
            httpHost.UseGraphQL();
            httpHost.UseStaticFiles(HubExplorer.Path); // optional - HubExplorer nuget: https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.Explorer
            // await CreateWebRtcServer(httpHost);
            
            var server = HttpHost.GetArg(args, "--server");
            // Hub Explorer: http://localhost:5000/fliox/
            switch (server) {
                case null:
                case "asp.net3":        StartupAsp3.Run(args, httpHost);                return; // ASP.NET Core 3, 3.1, 5
                case "HttpListener":    HttpServer.RunHost("http://localhost:5000/", httpHost); return; // HttpListener from BCL
            }
        }
        
        private static EntityDatabase CreateDatabase(string provider, DatabaseSchema schema)
        {
            var fileDb = new FileDatabase("main_db", "../Test/DB/main_db", schema);
            switch (provider) {
                case "file":    return fileDb;
                case "memory":  return new MemoryDatabase("main_db", schema).SeedDatabase(fileDb).Result;
            }
            throw new InvalidOperationException($"unknown provider: {provider}"); 
        }
        
        private static async Task CreateWebRtcServer(HttpHost httpHost) {
            var rtcConfig = new SignalingConfig {
                SignalingHost   = "ws://localhost:8011/fliox/",
                User            = "admin", Token = "admin",
                WebRtcConfig    = new WebRtcConfig { IceServerUrls = new [] { "stun:stun.sipsorcery.com" } },
            };
            var rtcServer = new RtcServer(rtcConfig);
            await rtcServer.AddHost("abc", httpHost);
        }
    }
}
