// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Host.Event;


namespace Friflo.Json.Fliox.Hub.Host
{
    public static class HubExtensions
    {
        public static void UseClusterDB(this FlioxHub hub) {
            hub.AddExtensionDB (new ClusterDB("cluster", hub)); // required by HubExplorer
        }
        
        public static void UseMonitorDB(this FlioxHub hub) {
            hub.AddExtensionDB (new MonitorDB("monitor", hub));
        }
        
        public static void UsePubSub(this FlioxHub hub, EventDispatching dispatching = EventDispatching.QueueSend) {
            hub.EventDispatcher     = new EventDispatcher(dispatching);   // enables Pub-Sub (sending events for subscriptions)
        }

        public static async Task UseUserDB(this FlioxHub hub, EntityDatabase userDB) {
            var authenticator   = new UserAuthenticator(userDB);
            await authenticator.SetAdminPermissions();                                  // optional - enable Hub access with user/token: admin/admin
            await authenticator.SetClusterPermissions("cluster", Users.All);
            await authenticator.SubscribeUserDbChanges(hub.EventDispatcher);            // optional - apply user_db changes instantaneously
            hub.AddExtensionDB(userDB);
            hub.Authenticator       = authenticator;
        }
    }
}