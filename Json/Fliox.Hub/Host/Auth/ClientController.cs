// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    /// <summary>
    /// <see cref="ClientController"/> us used to create unique client ids
    /// </summary>
    /// <remarks>
    /// It creates a unique client id for a given <see cref="User"/> by <see cref="NewClientIdFor"/> or
    /// checks if a given client id can be used (added) for a given <see cref="User"/> by <see cref="UseClientIdFor"/>.
    /// Multiple client ids can be added to a <see cref="User"/>. Once added to a <see cref="User"/> the client id
    /// cannot be used (added) by another <see cref="User"/>.
    /// <br/>
    /// A <see cref="ClientController"/> is used to:
    /// <list type="bullet">
    ///   <item> create / add unique client ids by <see cref="FlioxHub.ClientController"/> </item>
    ///   <item> enables sending Push messages (events) for protocols supporting this like WebSocket's </item>
    ///   <item> enables monitoring request / execution statistics of <see cref="FlioxHub.ExecuteRequestAsync"/> </item>
    /// </list>
    /// </remarks>
    public abstract class ClientController {
        /// key: clientId
        [DebuggerBrowsable(Never)]
        internal readonly   ConcurrentDictionary<ShortString, UserClient>   clients = new ConcurrentDictionary<ShortString, UserClient>(ShortString.Equality);
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<UserClient>                         Clients => clients.Values;

        public   override   string                                          ToString() => $"clients: {clients.Count}";
        
        protected abstract  ShortString     NewId();

        public ShortString NewClientIdFor(User user) {
            while (true) { 
                var clientId = NewId();
                var client = new UserClient(user.userId);
                if (clients.TryAdd(clientId, client)) {
                    user.clients.TryAdd(clientId, new Empty());
                    return clientId;
                }
            }
        }
        
        public bool UseClientIdFor(User user, in ShortString clientId) {
            if (clients.TryGetValue(clientId, out UserClient client )) {
                return client.userId.IsEqual(user.userId);
            }
            client = new UserClient(user.userId);
            if (clients.TryAdd(clientId, client)) {
                user.clients.TryAdd(clientId, new Empty());
                return true;
            }
            return false; 
        }
        
        internal void ClearClientStats() {
            foreach (var pair in clients) {
                var requestCounts = pair.Value.requestCounts;
                lock (requestCounts) {
                    requestCounts.Clear();
                }
            }
        }
    }
    
    public sealed class IncrementClientController : ClientController {
        private long clientIdSequence;

        protected override ShortString NewId() {
            var id = Interlocked.Increment(ref clientIdSequence);
            return new ShortString(id.ToString());
        }
    }
    
    public sealed class GuidClientController : ClientController {
        protected override ShortString NewId() {
            return new ShortString(Guid.NewGuid().ToString());
        }
    }
}