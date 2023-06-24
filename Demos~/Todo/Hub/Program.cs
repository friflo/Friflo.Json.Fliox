using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Todo;

namespace TodoHub
{
    internal  static class  Program
    {
        public static void Main()
        {
            var schema      = DatabaseSchema.Create<TodoClient>();
            var database    = new FileDatabase("main_db", "../Test/DB/main_db", schema); // uses records stored in 'main_db/jobs' folder
            var hub         = new FlioxHub(database);
            hub.UseClusterDB(); // required by HubExplorer
            hub.UsePubSub();    // optional - enables Pub-Sub
            // --- create HttpHost
            var httpHost    = new HttpHost(hub, "/fliox/");
            httpHost.UseStaticFiles(HubExplorer.Path); // HubExplorer nuget: https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.Explorer
            HttpServer.RunHost("http://+:8010/", httpHost);
        }
    }
}
