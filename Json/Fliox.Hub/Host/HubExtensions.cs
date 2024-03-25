// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Host.Event;

namespace Friflo.Json.Fliox.Hub.Host
{
    public static class HubExtensions
    {
        /// <summary>
        /// Add the <b>cluster</b> database to the given <paramref name="hub"/> providing meta information about hosted databases.
        /// </summary>
        /// <remarks>
        /// This meta information is used by the <b>Hub.Explorer</b> UI to browse hosted databases.<br/>
        /// The <b>Hub.Explorer</b> UI needs to be added to the <see cref="Remote.HttpHost"/> with
        /// <br/>
        /// <code>
        /// httpHost.UseStaticFiles(HubExplorer.Path); // nuget: https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.Explorer
        /// </code>
        /// <br/>
        /// <i>Info</i>: to access the <b>cluster</b> database use a <see cref="ClusterStore"/> client.
        /// </remarks>
        public static void UseClusterDB(this FlioxHub hub) {
            hub.AddExtensionDB (new ClusterDB("cluster", hub)); // required by HubExplorer
        }
        
        /// <summary>
        /// Add the <b>monitor</b> database to the given <paramref name="hub"/> providing access information about
        /// of the Hub and its databases.
        /// </summary>
        /// <remarks>
        /// Access information:<br/>
        /// - request and task count executed per user <br/>
        /// - request and task count executed per client. A user can access without, one or multiple client ids. <br/>
        /// - events sent to (or buffered for) clients subscribed by these clients. <br/>
        /// - aggregated access counts of the Hub in the last 30 seconds and 30 minutes.<br/>
        /// <br/>
        /// <i>Info</i>: to access the <b>monitor</b> database use a <see cref="MonitorStore"/> client.
        /// </remarks>
        public static void UseMonitorDB(this FlioxHub hub) {
            hub.AddExtensionDB (new MonitorDB("monitor", hub));
        }
        
        /// <summary>
        /// Assign an <see cref="FlioxHub.EventDispatcher"/> to the given <paramref name="hub"/> to enable <b>Pub-Sub</b>.
        /// </summary>
        /// <remarks>
        /// It enables sending push events to clients for database changes and messages these clients have subscribed. <br/>
        /// In case of remote database connections <b>WebSockets</b> are used to send push events to clients.<br/>
        /// </remarks>
        public static void UsePubSub(this FlioxHub hub, EventDispatching dispatching = EventDispatching.QueueSend) {
            hub.EventDispatcher     = new EventDispatcher(dispatching);   // enables Pub-Sub (sending events for subscriptions)
        }

        /// <summary>
        /// Add the <b>user_db</b> database to the given <paramref name="hub"/> to control individual user access
        /// to databases their containers and commands.
        /// </summary>
        /// <remarks>
        /// - Each <b>user</b> has a set of <b>roles</b> stored in container <b>permissions</b>. <br/>
        /// - Each <b>role</b> in container <b>roles</b> has a set of <b>rights</b> which grant or deny container access or command execution.<br/>
        /// <br/>
        /// <i>Requirement:</i> the given <paramref name="userDB"/> must be created with
        /// the <c>schema</c> <see cref="UserDB.Schema"/> and the <c>service</c> <see cref="UserDBService"/> instance. E.g.
        /// <code>
        /// new FileDatabase("user_db", "../Test/DB/user_db", UserDB.Schema, new UserDBService())
        /// </code>
        /// <br/> 
        /// <i>Info:</i> to access the <b>user_db</b> database use a <see cref="UserStore"/> client.
        /// </remarks>
        public static async Task UseUserDB(
            this FlioxHub   hub,
            EntityDatabase  userDB,
            string          adminToken  = "admin",
            Users           users       = Users.All)
        {
            if (userDB.Schema != UserDB.Schema) {
                throw new ArgumentException("expect database schema: UserDB.Schema", nameof(userDB));
            }
            if (userDB.service.GetType() != typeof(UserDBService)) {
                throw new ArgumentException("expect database service: new UserDBService()", nameof(userDB));
            }
            var authenticator = new UserAuthenticator(userDB);
            await authenticator.SetAdminPermissions(adminToken).ConfigureAwait(false);              // optional - enable Hub access with user/token: admin/admin
            await authenticator.SetClusterPermissions("cluster", users).ConfigureAwait(false);
            await authenticator.SubscribeUserDbChanges(hub.EventDispatcher).ConfigureAwait(false);  // optional - apply user_db changes instantaneously
            hub.AddExtensionDB(userDB);
            hub.Authenticator       = authenticator;
        }
    }
}