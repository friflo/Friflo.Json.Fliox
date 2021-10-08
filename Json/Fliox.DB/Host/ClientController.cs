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
        public readonly HashSet<JsonKey> clients = new HashSet<JsonKey>(JsonKey.Equality);

        public JsonKey NewClientId() {
            var id = NewId();
            clients.Add(id);
            return id;
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