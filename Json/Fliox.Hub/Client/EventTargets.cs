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
        internal    List<JsonKey>           users;
        internal    List<EventTargetClient> clients;
        
        public void AddUser(string user) {
            AddUser (new JsonKey(user));
        }
        
        public void AddUser(JsonKey user) {
            if (users == null) {
                users = new List<JsonKey> { user };
                return;
            }
            users.Add(user);
        }
        
        public void AddClient(JsonKey user, JsonKey client) {
            AddClient (new EventTargetClient(user, client));
        }

        public void AddClient(EventTargetClient client) {
            if (clients == null) {
                clients = new List<EventTargetClient> { client };
                return;
            }
            clients.Add(client);
        }
        
        public void AddUsers (ICollection<string> users) {
            if (this.users == null) this.users = new List<JsonKey>(users.Count);
            this.users.AddRange(this.users);
        }
        
        public void AddUsers (ICollection<JsonKey> users) {
            if (this.users == null) this.users = new List<JsonKey>(users.Count);
            this.users.AddRange(this.users);
        }
        
        public void AddClients (ICollection<(string, string)> userClients) {
            if (this.clients == null) this.clients = new List<EventTargetClient>(userClients.Count);
            foreach (var (user, client) in userClients) {
                this.clients.Add(new EventTargetClient(user, client));
            }
        }
        
        internal void AddClients (ICollection<EventTargetClient> userClients) {
            if (this.clients == null) this.clients = new List<EventTargetClient>(userClients.Count);
            foreach (var element in userClients) {
                this.clients.Add(element);
            }
        }
    }
}