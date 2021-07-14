// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.UserAuth
{
    public class UserStore : EntityStore
    {
        public readonly EntitySet<UserRole>         roles;
        public readonly EntitySet<UserCredential>   credentials;
        
        public UserStore(EntityDatabase database, string clientId) : base(database, SyncTypeStore.Get(), clientId) {
            roles       = new EntitySet<UserRole>       (this);
            credentials = new EntitySet<UserCredential> (this);
        }
        
        public void InitUserDatabase (EntityDatabase database) {
            database.authenticator = new ValidationAuthenticator();
            database.taskHandler.AddCommandHandlerAsync<AuthenticateUser, AuthenticateUserResult>(ValidateTokenHandler); 
        }
        
        private async Task<AuthenticateUserResult> ValidateTokenHandler (Command<AuthenticateUser> command) {
            var validateToken   = command.Value;
            var clientId        = validateToken.clientId;
            var readCredentials = credentials.Read();
            var findCred        = readCredentials.Find(clientId);
            var readRoles       = roles.Read();
            var findRole        = readRoles.Find(clientId);
            
            await Sync();
                
            UserCredential  cred = findCred.Result;
            UserRole        role = findRole.Result;
            bool            isValid = cred != null && cred.token == validateToken.token;
            return new AuthenticateUserResult { isValid = isValid, roles = role?.roles };
        }
        
        internal async Task<AuthenticateUserResult> AuthenticateUser(AuthenticateUser command) {
            var commandTask = SendMessage<AuthenticateUser, AuthenticateUserResult>(command);
            await Sync();
            return commandTask.Result;
        }
    }

    // -------------------------------------- models ---------------------------------------
    public class UserRole : Entity {
        public  List<string> roles;
    }
    
    public class UserCredential : Entity {
        public  string      passHash;
        public  string      token;
    }
    
    // -------------------------------------- commands -------------------------------------
    public class AuthenticateUser {
        public          string  clientId;
        public          string  token;

        public override string  ToString() => clientId;
    }
    
    public class AuthenticateUserResult {
        public          bool            isValid;
        public          List<string>    roles;
    }
}