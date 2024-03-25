// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    /// <summary>
    /// Define the general Hub permissions. For now this apply only to event processing. <see cref="queueEvents"/>  
    /// </summary>
    /// <remarks>
    /// Task specific permissions are defined by <see cref="TaskAuthorizer"/>.
    /// </remarks>
    public sealed class HubPermission
    {
        /// <summary>
        /// If true events are stored for user client to resent unacknowledged events on reconnects.
        /// <see cref="Event.EventSubClient.queueEvents"/>
        /// </summary>
        public readonly bool queueEvents;

        public static readonly HubPermission Full = new HubPermission (true);
        public static readonly HubPermission None = new HubPermission (false);
        
        public HubPermission(bool queueEvents) {
            this.queueEvents = queueEvents;
        }
    }
}