// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Auth
{
    public class AuthClient {
        internal readonly   JsonKey             userId;
        internal            int                 requests;
        internal            int                 tasks;
        
        public   override   string              ToString() => userId.AsString();

        internal AuthClient (in JsonKey userId) {
            this.userId     = userId;
        }
    }
    
    /// <summary>
    /// Create a unique client id for a given user by <see cref="NewClientIdFor"/> or
    /// checks if a given client id can be added to a given user by <see cref="AddClientIdFor"/> 
    /// Its used to:
    /// <list type="bullet">
    ///   <item> create / add unique client ids by <see cref="EntityDatabase.clientController"/> </item>
    ///   <item> enables monitoring execution statistics of <see cref="EntityDatabase.ExecuteSync"/> </item>
    /// </list>
    /// </summary>
    public abstract class ClientController {
        /// key: clientId
        private readonly    Dictionary<JsonKey, AuthClient>            clients = new Dictionary<JsonKey, AuthClient>(JsonKey.Equality);
        public              IReadOnlyDictionary<JsonKey, AuthClient>   Clients => clients;

        public JsonKey NewClientIdFor(AuthUser authUser) {
            while (true) { 
                var clientId = NewId();
                var client = new AuthClient(authUser.userId);
                if (clients.TryAdd(clientId, client)) {
                    authUser.clients.Add(clientId);
                    return clientId;
                }
            }
        }
        
        public bool AddClientIdFor(AuthUser authUser, in JsonKey clientId) {
            var client = new AuthClient(authUser.userId);
            if (clients.TryAdd(clientId, client)) {
                authUser.clients.Add(clientId);
                return true;
            }
            return false; 
        }
        
        protected abstract JsonKey NewId();
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