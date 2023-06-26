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
            var httpHost    = new HttpHost(hub, "/fliox/");
            HttpServer.RunDevHost("http://+:8010/", httpHost, HubExplorer.Path);
        }
    }
}
