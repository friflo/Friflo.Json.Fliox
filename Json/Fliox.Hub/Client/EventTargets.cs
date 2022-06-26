// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Models;

// ReSharper disable ClassNeverInstantiated.Global
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary> prototype WIP </summary>
    public struct EventTargets
    {
        internal IList<EventTargetUser>   users;
        
        public EventTargets (string user) {
            users = new List<EventTargetUser> { new EventTargetUser(user) };
        }
        
        public EventTargets (JsonKey user) {
            users = new List<EventTargetUser> { new EventTargetUser(user) };
        }
        
        public EventTargets (string user, string  client) {
            users = new List<EventTargetUser> { new EventTargetUser(user, client) };
        }
        
        public EventTargets (JsonKey user, JsonKey  client) {
            users = new List<EventTargetUser> { new EventTargetUser(user, client) };
        }
        
        public EventTargets (EventTargetUser userClient) {
            users = new List<EventTargetUser> { userClient };
        }
        
        public void AddUser(string user, string client = null) {
            AddClient (new EventTargetUser(user, client));
        }
        
        public void AddUser(JsonKey user, JsonKey client) {
            AddClient (new EventTargetUser(user, client));
        }

        public void AddClient(EventTargetUser client) {
            if (users == null) {
                users = new List<EventTargetUser> { client };
                return;
            }
            users.Add(client);
        }
        
        public void AddClients (ICollection<string> users) {
            if (this.users == null) this.users = new List<EventTargetUser>(users.Count);
            foreach (var user in users) {
                this.users.Add(new EventTargetUser(user));
            }
        }
        
        public void AddClients (ICollection<JsonKey> users) {
            if (this.users == null) this.users = new List<EventTargetUser>(users.Count);
            foreach (var user in users) {
                this.users.Add(new EventTargetUser(user));
            }
        }
        
        public void AddClients (ICollection<(string, string)> userClients) {
            if (this.users == null) this.users = new List<EventTargetUser>(userClients.Count);
            foreach (var (user, client) in userClients) {
                this.users.Add(new EventTargetUser(user, client));
            }
        }
        
        internal void AddClients (ICollection<EventTargetUser> userClients) {
            if (this.users == null) this.users = new List<EventTargetUser>(userClients.Count);
            foreach (var element in userClients) {
                this.users.Add(element);
            }
        }
    }
}