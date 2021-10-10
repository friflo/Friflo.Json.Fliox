// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host
{
    /// <summary>
    /// Create a unique id when calling <see cref="NewId"/>.
    /// Its used to create unique client ids by <see cref="EntityDatabase.clientController"/>
    /// </summary>
    public abstract class ClientController {
        /// key: clientId, value: userId
        private readonly    Dictionary<JsonKey, JsonKey>            clients = new Dictionary<JsonKey, JsonKey>(JsonKey.Equality);
        public              IReadOnlyDictionary<JsonKey, JsonKey>   Clients => clients;

        public JsonKey NewClientIdFor(in JsonKey userId) {
            while (true) { 
                var id = NewId();
                if (clients.TryAdd(id, userId))
                    return id;
            }
        }
        
        public bool AddClientIdFor(in JsonKey userId, in JsonKey clientId) {
            return clients.TryAdd(clientId, userId);
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