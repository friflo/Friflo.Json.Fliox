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
        /// Blueprint showing how to setup a <see cref="HttpHost"/> utilizing all features available
        /// via HTTP and WebSockets with ASP.NET Core 6.0.
        /// See: https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Fliox.Hub/Host/README.md#httphost
        /// </summary>
        public static async Task Main(string[] args)
        {
            var schema      = DatabaseSchema.Create<DemoClient>(); // create TypeSchema from Type
            var seedSource  = new FileDatabase("main_db", "../Test/DB/main_db", schema);
            var database    = await new MemoryDatabase("main_db", schema).SeedDatabase(seedSource);
            database.AddCommands(new DemoCommands());
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
            
            Startup.Run(args, httpHost); // ASP.NET Core 6
            // HttpServer.RunHost("http://+:8010/", httpHost); 
        }
    }
}
