// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
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
        internal    List<ShortString>   users;
        internal    List<ShortString>   clients;
        internal    List<ShortString>   groups;
        
        // --- user
        public void AddUser(string user) {
            if (user == null) throw new ArgumentNullException(nameof(user));
            AddUser (new ShortString(user));
        }
        
        public void AddUser(in ShortString user) {
            if (user.IsNull()) throw new ArgumentNullException(nameof(user));
            if (users == null) {
                users = new List<ShortString> { user };
                return;
            }
            users.Add(user);
        }
        
        // --- users
        public void AddUsers (ICollection<string> users) {
            if (this.users == null) this.users = new List<ShortString>(users.Count);
            foreach (var user in users) {
                this.users.Add(new ShortString(user));
            }
        }
        
        public void AddUsers (ICollection<ShortString> users) {
            if (this.users == null) this.users = new List<ShortString>(users.Count);
            this.users.AddRange(users);
        }
        
        // --- client
        public void AddClient(string client) {
            if (client == null) throw new ArgumentNullException(nameof(client));
            AddClient (new ShortString(client));
        }

        public void AddClient(in ShortString client) {
            if (client.IsNull()) throw new ArgumentNullException(nameof(client));
            if (clients == null) {
                clients = new List<ShortString> { client };
                return;
            }
            clients.Add(client);
        }
        
        // --- clients
        public void AddClients (ICollection<string> clients) {
            if (this.clients == null) this.clients = new List<ShortString>(clients.Count);
            foreach (var client in clients) {
                this.clients.Add(new ShortString(client));
            }
        }
        
        internal void AddClients (ICollection<ShortString> clients) {
            if (this.clients == null) this.clients = new List<ShortString>(clients.Count);
            this.clients.AddRange(clients);
        }
        
        // --- group
        public void AddGroup(string group) {
            if (group == null) throw new ArgumentNullException(nameof(group));
            if (groups == null) {
                groups = new List<ShortString> { new ShortString(group) };
                return;
            }
            groups.Add(new ShortString(group));
        }
        
        // --- groups
        public void AddGroups(ICollection<string> groups) {
            if (this.groups == null) this.groups = new List<ShortString>(groups.Count);
            foreach (var group in groups) {
                this.groups.Add(new ShortString(group));
            }
        }
        
        public void AddGroups(ICollection<ShortString> groups) {
            if (this.groups == null) this.groups = new List<ShortString>(groups.Count);
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
        public static  TTask  EventTargetUser<TTask> (this TTask message, in ShortString user) where TTask : MessageTask {
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
        public static  TTask  EventTargetUsers<TTask> (this TTask message, ICollection<ShortString> users) where TTask : MessageTask {
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
        public static  TTask  EventTargetClient<TTask> (this TTask message, in ShortString client) where TTask : MessageTask {
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
        public static  TTask  EventTargetClients<TTask> (this TTask message, ICollection<ShortString>  clients) where TTask : MessageTask {
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