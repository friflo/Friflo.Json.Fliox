using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Native;

namespace Fliox.TodoHub
{
    /// <summary>Bootstrapping of databases hosted by <see cref="HttpHost"/></summary> 
    internal  static class  Program
    {
        public static void Main() {
            var hostHub = CreateHttpHost();
            HttpListenerHost.RunHost("http://+:8010/", hostHub);
        }

        /// <summary>
        /// This method is a blueprint showing how to setup a <see cref="HttpHost"/> utilizing a minimal features set
        /// available via HTTP and WebSockets
        /// </summary>
        private static HttpHost CreateHttpHost() {
            var typeSchema          = NativeTypeSchema.Create(typeof(TodoStore)); // optional - create TypeSchema from Type
            var database            = new FileDatabase("main_db", "./DB~/main_db");
            database.Schema         = new DatabaseSchema(typeSchema);

            var hub                 = new FlioxHub(database);
            hub.Info.projectName    = "TodoHub";                    // optional
            hub.AddExtensionDB (new ClusterDB("cluster", hub));     // optional - expose info of hosted databases. Required by Hub Explorer
            hub.EventDispatcher     = new EventDispatcher(true);    // optional - enables sending events for subscriptions
            
            var httpHost            = new HttpHost(hub, "/fliox/");
            httpHost.AddHandler      (new StaticFileHandler(HubExplorer.Path)); // optional - serve static web files of Hub Explorer
            return httpHost;
        }
    }
}
