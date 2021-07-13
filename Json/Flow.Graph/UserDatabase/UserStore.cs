// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.UserDatabase
{
    public class UserStore : EntityStore, ITokenValidator
    {
        public readonly EntitySet<UserRole>         roles;
        public readonly EntitySet<UserCredential>   credentials;
        
        public UserStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            roles       = new EntitySet<UserRole>       (this);
            credentials = new EntitySet<UserCredential> (this);
        }
        
        public void AddCommandHandler (TaskHandler taskHandler) {
            taskHandler.AddCommandHandlerAsync<ValidateToken, ValidateTokenResult>(ValidateTokenHandler); 
        }
        
        private async Task<ValidateTokenResult> ValidateTokenHandler (Command<ValidateToken> command) {
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
            return new ValidateTokenResult { isValid = isValid, roles = role?.roles };
        }
        
        public async Task<ValidateTokenResult> ValidateToken(string clientId, string token) {
            var command = new ValidateToken { clientId = clientId, token = token };
            var commandTask = SendMessage<ValidateToken, ValidateTokenResult>(command);
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
    public class ValidateToken {
        public          string  clientId;
        public          string  token;

        public override string  ToString() => clientId;
    }
    
    public class ValidateTokenResult {
        public          bool            isValid;
        public          List<string>    roles;
    }
}