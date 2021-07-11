// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Database.Auth
{
    public class UserStore : EntityStore
    {
        public readonly EntitySet<UserProfile> users;
        
        public UserStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            users = new EntitySet<UserProfile>(this);
        }
    }
    
    public class UserProfile : Entity {
        public string   name;
        public string   passwordHash;
    }
    
    public class UserAuthenticator : ClientAuthenticator
    {
        private readonly UserStore userStore;

        public UserAuthenticator (UserStore userStore) {
            this.userStore = userStore;
        }
        
        
    }
}