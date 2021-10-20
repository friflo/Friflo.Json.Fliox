// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Auth
{
    /// <summary>
    /// Create a unique client id for a given <see cref="User"/> by <see cref="NewClientIdFor"/> or
    /// checks if a given client id can be used (added) for a given <see cref="User"/> by <see cref="UseClientIdFor"/>.
    /// Multiple client ids can be added to a <see cref="User"/>. Once added to a <see cref="User"/> is cannot be
    /// used (added) by another <see cref="User"/>.
    /// 
    /// <see cref="ClientController"/> is used to:
    /// <list type="bullet">
    ///   <item> create / add unique client ids by <see cref="DatabaseHub.ClientController"/> </item>
    ///   <item> enables sending Push messages (events) for protocols supporting this like WebSocket's </item>
    ///   <item> enables monitoring request / execution statistics of <see cref="DatabaseHub.ExecuteSync"/> </item>
    /// </list>
    /// </summary>
    public abstract class ClientController {
        /// key: clientId
        internal readonly   Dictionary<JsonKey, UserClient>            clients = new Dictionary<JsonKey, UserClient>(JsonKey.Equality);
        public              IReadOnlyDictionary<JsonKey, UserClient>   Clients => clients;
        
        protected abstract  JsonKey     NewId();

        public JsonKey NewClientIdFor(User user) {
            while (true) { 
                var clientId = NewId();
                var client = new UserClient(user.userId);
                if (clients.TryAdd(clientId, client)) {
                    user.clients.Add(clientId);
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
                user.clients.Add(clientId);
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
    
    public class IncrementClientController : ClientController {
        private long clientIdSequence;

        protected override JsonKey NewId() {
            var id = Interlocked.Increment(ref clientIdSequence);
            return new JsonKey(id);
        }
    }
    
    public class GuidClientController : ClientController {
        protected override JsonKey NewId() {
            return new JsonKey(Guid.NewGuid());
        }
    }
}