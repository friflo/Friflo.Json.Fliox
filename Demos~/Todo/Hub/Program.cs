using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Native;
using Todo;

namespace TodoHub
{
    /// <summary>Bootstrapping of databases hosted by <see cref="HttpHost"/></summary> 
    internal  static class  Program
    {
        public static void Main()
        {
            var httpHost = CreateHttpHost();
            HttpListenerHost.RunHost("http://+:8010/", httpHost);
        }
        
        /// <summary>
        /// This method is a blueprint showing how to setup a <see cref="HttpHost"/> utilizing a minimal features set
        /// available via HTTP and WebSockets
        /// </summary>
        private static HttpHost CreateHttpHost()
        {
            var typeSchema          = NativeTypeSchema.Create(typeof(TodoClient));
            var database            = new FileDatabase("main_db", "../Test/DB/main_db");
            database.Schema         = new DatabaseSchema(typeSchema);

            var hub                 = new FlioxHub(database);
            hub.Info.projectName    = "TodoHub";
            hub.AddExtensionDB (new ClusterDB("cluster", hub)); // required by HubExplorer
            
            var httpHost            = new HttpHost(hub, "/fliox/");
            httpHost.AddHandler      (new StaticFileHandler(HubExplorer.Path));
            return httpHost;
        }
    }
}
