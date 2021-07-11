// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
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
        public string   token;
    }
    
    public class UserAuthenticator : Authenticator
    {
        private readonly UserStore userStore;
        
        public UserAuthenticator (UserStore userStore) {
            this.userStore = userStore;
        }
        
        public override async Task<string> GetClientToken(string clientId) {
            var readUser = userStore.users.Read();
            var findUserProfile = readUser.Find(clientId);
            await userStore.Sync();
            
            UserProfile userProfile = findUserProfile.Result;
            if (userProfile == null)
                return null;
            return userProfile.token;
        }
    }
}