// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    /// <summary>
    /// An <see cref="EventSubUser"/> - Event Subscriber User - store the <see cref="groups"/> assigned to a <see cref="userId"/>. <br/>
    /// The <see cref="groups"/> are used to restrict forwarding message events only to the users part of a specific group.   
    /// </summary>
    internal sealed class EventSubUser
    {
        internal  readonly  ShortString                                 userId;
        // A ConcurrentHashSet<> would be sufficient
        internal  readonly  ConcurrentDictionary<EventSubClient, bool>  clients;
        internal  readonly  HashSet<ShortString>                        groups; // never null

        public    override  string                                      ToString() => $"user: {userId.AsString()}";

        internal EventSubUser (in ShortString userId, IEnumerable<ShortString> groups) {
            this.clients    = new ConcurrentDictionary<EventSubClient, bool>();
            this. groups    = new HashSet<ShortString>(ShortString.Equality);
            this.userId = userId;
            this.groups.UnionWith(groups);
        }
    }
}