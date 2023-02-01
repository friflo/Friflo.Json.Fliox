using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Native;
using Todo;

namespace TodoHub
{
    internal  static class  Program
    {
        public static void Main()
        {
            var httpHost = CreateHttpHost();
            HttpServer.RunHost("http://+:8010/", httpHost);
        }
        
        /// <summary> blueprint to showcase a minimal feature set of a <see cref="HttpHost"/> </summary>
        private static HttpHost CreateHttpHost()
        {
            var database        = new FileDatabase("main_db", "../Test/DB/main_db"); // uses records stored in 'main_db/jobs' folder
            var typeSchema      = NativeTypeSchema.Create(typeof(TodoClient));
            database.Schema     = new DatabaseSchema(typeSchema);

            var hub             = new FlioxHub(database);
            hub.Info.projectName= "TodoHub";
            hub.AddExtensionDB (new ClusterDB("cluster", hub)); // required by HubExplorer
            hub.EventDispatcher = new EventDispatcher(EventDispatching.QueueSend);    // optional - enables Pub-Sub
            
            var httpHost        = new HttpHost(hub, "/fliox/");
            httpHost.AddHandler (new StaticFileHandler(HubExplorer.Path));
            return httpHost;
        }
    }
}
