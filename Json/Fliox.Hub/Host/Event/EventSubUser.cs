// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal sealed class EventSubUser
    {
        internal  readonly  JsonKey                 userId;  
        internal  readonly  HashSet<EventSubClient> clients = new HashSet<EventSubClient>();
        internal  readonly  HashSet<string>         groups  = new HashSet<string>();

        public    override  string                  ToString() => $"user: {userId.AsString()}";

        internal EventSubUser (in JsonKey userId, ICollection<string> groups) {
            this.userId = userId;
            if (groups != null) this.groups.UnionWith(groups);
        }
    }
}