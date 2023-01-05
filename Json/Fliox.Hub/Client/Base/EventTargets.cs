// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    // ---------------------------------------------- EventTargets ----------------------------------------------
    /// <summary>
    /// <see cref="EventTargets"/> instructs the Hub to forward messages as events only to the specified targets.
    /// Event target's can be specified its <b>users, clients or groups</b>.
    /// </summary>
    /// <remarks>
    /// In case no targets are specified - the default - a message is sent to all clients subscribing the message.
    /// </remarks>
    public sealed class EventTargets
    {
        internal    List<JsonKey>   users;
        internal    List<JsonKey>   clients;
        internal    List<string>    groups;
        
        // --- user
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
        
        // --- users
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
        
        // --- client
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
        
        // --- clients
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
        
        // --- group
        public void AddGroup(string group) {
            if (groups == null) {
                groups = new List<string> { group };
                return;
            }
            groups.Add(group);
        }
        
        public void AddGroups(ICollection<string> groups) {
            if (this.groups == null) this.groups = new List<string>(groups.Count);
            this.groups.AddRange(groups);
        }
    }


    // ------------------------------------------ EventTargetsExtension ------------------------------------------
    public static class EventTargetsExtension
    {
        // --- user
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="user"/> </summary>
        /// <seealso cref="EventTargets"/>
        public static  TTask  EventTargetUser<TTask> (this TTask message, string  user) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddUser(user);
            return message;
        }
        
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="user"/> </summary>
        /// <seealso cref="EventTargets"/>
        public static  TTask  EventTargetUser<TTask> (this TTask message, in JsonKey user) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddUser(user);
            return message;
        }
        
        // --- users
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="users"/> </summary>
        /// <seealso cref="EventTargets"/>
        public static  TTask  EventTargetUsers<TTask> (this TTask message, ICollection<string> users) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddUsers(users);
            return message;
        }
        
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="users"/> </summary>
        /// <seealso cref="EventTargets"/>
        public static  TTask  EventTargetUsers<TTask> (this TTask message, ICollection<JsonKey> users) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddUsers (users);
            return message;
        }
        
        // --- client
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="client"/> </summary>
        /// <seealso cref="EventTargets"/>
        public static  TTask  EventTargetClient<TTask> (this TTask message, string client) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddClient(client);
            return message;
        }
        
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="client"/> </summary>
        /// <seealso cref="EventTargets"/>
        public static  TTask  EventTargetClient<TTask> (this TTask message, in JsonKey client) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddClient(client);
            return message;
        }

        // --- clients
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="clients"/> </summary>
        /// <seealso cref="EventTargets"/>
        public static  TTask  EventTargetClients<TTask> (this TTask message, ICollection<string>  clients) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddClients(clients);
            return message;
        }
        
        /// <summary> Send the <paramref name="message"/> as an event only to the given <paramref name="clients"/> </summary>
        /// <seealso cref="EventTargets"/>
        public static  TTask  EventTargetClients<TTask> (this TTask message, ICollection<JsonKey>  clients) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddClients (clients);
            return message;
        }
        
        // --- group
        public static  TTask  EventTargetGroup<TTask> (this TTask message, string group) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddGroup(group);
            return message;
        }
        
        // --- groups
        public static  TTask  EventTargetGroups<TTask> (this TTask message, ICollection<string> groups) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddGroups(groups);
            return message;
        }
    }
}