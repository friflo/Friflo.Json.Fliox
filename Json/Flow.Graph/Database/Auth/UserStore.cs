// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Database.Auth
{
    public class UserStore : EntityStore
    {
        public readonly EntitySet<UserRole>         roles;
        public readonly EntitySet<UserCredential>   credentials;
        
        public UserStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            roles       = new EntitySet<UserRole>(this);
            credentials = new EntitySet<UserCredential>(this);
        }
    }
    
    public class UserRole : Entity {
        public List<string> roles;
    }
    
    public class UserCredential : Entity {
        public string       passwordHash;
        public string       token;
    }
}