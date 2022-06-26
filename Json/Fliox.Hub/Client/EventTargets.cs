// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable ClassNeverInstantiated.Global
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary> prototype WIP </summary>
    public struct EventTargets
    {
        internal    List<JsonKey>   users;
        internal    List<JsonKey>   clients;
        
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
        
        public void AddClient(string client) {
            AddClient (new JsonKey(client));
        }

        public void AddClient(JsonKey client) {
            if (clients == null) {
                clients = new List<JsonKey> { client };
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
        
        public void AddClients (ICollection<string> clients) {
            if (this.clients == null) this.clients = new List<JsonKey>(clients.Count);
            foreach (var client in clients) {
                this.clients.Add(new JsonKey(client));
            }
        }
        
        internal void AddClients (ICollection<JsonKey> clients) {
            if (this.clients == null) this.clients = new List<JsonKey>(clients.Count);
            foreach (var client in clients) {
                this.clients.Add(client);
            }
        }
    }
}