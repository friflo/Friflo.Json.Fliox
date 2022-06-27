// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable ClassNeverInstantiated.Global
namespace Friflo.Json.Fliox.Hub.Client
{
    // ---------------------------------------------- EventTargets ----------------------------------------------
    /// <summary> prototype WIP </summary>
    public class EventTargets
    {
        internal    List<JsonKey>   users;
        internal    List<JsonKey>   clients;
        
        public void AddUser(string user) {
            AddUser (new JsonKey(user));
        }
        
        public void AddUser(in JsonKey user) {
            if (users == null) {
                users = new List<JsonKey> { user };
                return;
            }
            users.Add(user);
        }
        
        public void AddClient(string client) {
            AddClient (new JsonKey(client));
        }

        public void AddClient(in JsonKey client) {
            if (clients == null) {
                clients = new List<JsonKey> { client };
                return;
            }
            clients.Add(client);
        }
        
        public void AddUsers (ICollection<string> users) {
            if (this.users == null) this.users = new List<JsonKey>(users.Count);
            foreach (var user in users) {
                this.users.Add(new JsonKey(user));
            }
        }
        
        public void AddUsers (ICollection<JsonKey> users) {
            if (this.users == null) this.users = new List<JsonKey>(users.Count);
            this.users.AddRange(users);
        }
        
        public void AddClients (ICollection<string> clients) {
            if (this.clients == null) this.clients = new List<JsonKey>(clients.Count);
            foreach (var client in clients) {
                this.clients.Add(new JsonKey(client));
            }
        }
        
        internal void AddClients (ICollection<JsonKey> clients) {
            if (this.clients == null) this.clients = new List<JsonKey>(clients.Count);
            this.clients.AddRange(clients);
        }
    }


    // ------------------------------------------ EventTargetsExtension ------------------------------------------
    public static class EventTargetsExtension
    {
        // --- user
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="user"/> </summary>
        public static  TTask  EventTargetUser<TTask> (this TTask message, string  user) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddUser(user);
            return message;
        }
        
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="user"/> </summary>
        public static  TTask  EventTargetUser<TTask> (this TTask message, in JsonKey user) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddUser(user);
            return message;
        }
        
        // --- client
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="client"/> </summary>
        public static  TTask  EventTargetClient<TTask> (this TTask message, string client) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddClient(client);
            return message;
        }
        
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="client"/> </summary>
        public static  TTask  EventTargetClient<TTask> (this TTask message, in JsonKey client) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddClient(client);
            return message;
        }
        
        // --- users
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="users"/> </summary>
        public static  TTask  EventTargetUsers<TTask> (this TTask message, ICollection<string> users) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddUsers(users);
            return message;
        }
        
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="users"/> </summary>
        public static  TTask  EventTargetUsers<TTask> (this TTask message, ICollection<JsonKey> users) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddUsers (users);
            return message;
        }
        
        // --- clients
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="clients"/> </summary>
        public static  TTask  EventTargetClients<TTask> (this TTask message, ICollection<string>  clients) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddClients(clients);
            return message;
        }
        
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="clients"/> </summary>
        public static  TTask  EventTargetClients<TTask> (this TTask message, ICollection<JsonKey>  clients) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddClients (clients);
            return message;
        }
    }
}