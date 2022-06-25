// Copyright (c) Ullrich Praetz. All rights reserved.
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
    /// Create a unique client id for a given <see cref="User"/> by <see cref="NewClientIdFor"/> or
    /// checks if a given client id can be used (added) for a given <see cref="User"/> by <see cref="UseClientIdFor"/>.
    /// Multiple client ids can be added to a <see cref="User"/>. Once added to a <see cref="User"/> the client id
    /// cannot be used (added) by another <see cref="User"/>.
    /// <br/>
    /// A <see cref="ClientController"/> is used to:
    /// <list type="bullet">
    ///   <item> create / add unique client ids by <see cref="FlioxHub.ClientController"/> </item>
    ///   <item> enables sending Push messages (events) for protocols supporting this like WebSocket's </item>
    ///   <item> enables monitoring request / execution statistics of <see cref="FlioxHub.ExecuteSync"/> </item>
    /// </list>
    /// </summary>
    public abstract class ClientController {
        /// key: clientId
        [DebuggerBrowsable(Never)]
        internal readonly   ConcurrentDictionary<JsonKey, UserClient>   clients = new ConcurrentDictionary<JsonKey, UserClient>(JsonKey.Equality);
        /// expose <see cref="clients"/> as property to show them as list in Debugger
        // ReSharper disable once UnusedMember.Local
        private             ICollection<UserClient>                     Clients => clients.Values;

        public   override   string                                      ToString() => $"clients: {clients.Count}";
        
        protected abstract  JsonKey     NewId();

        public JsonKey NewClientIdFor(User user) {
            while (true) { 
                var clientId = NewId();
                var client = new UserClient(user.userId);
                if (clients.TryAdd(clientId, client)) {
                    user.clients.TryAdd(clientId, new Empty());
                    return clientId;
                }
            }
        }
        
        public bool UseClientIdFor(User user, in JsonKey clientId) {
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
                pair.Value.requestCounts.Clear();
            }
        }
    }
    
    public sealed class IncrementClientController : ClientController {
        private long clientIdSequence;

        protected override JsonKey NewId() {
            var id = Interlocked.Increment(ref clientIdSequence);
            return new JsonKey(id);
        }
    }
    
    public sealed class GuidClientController : ClientController {
        protected override JsonKey NewId() {
            return new JsonKey(Guid.NewGuid());
        }
    }
}