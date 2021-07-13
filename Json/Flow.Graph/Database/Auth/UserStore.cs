// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Database.Auth
{
    public class UserStore : EntityStore
    {
        public readonly EntitySet<UserRole>         roles;
        public readonly EntitySet<UserCredential>   credentials;
        
        public UserStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            roles       = new EntitySet<UserRole>       (this);
            credentials = new EntitySet<UserCredential> (this);
        }
        
        public void AddCommandHandler (TaskHandler taskHandler) {
            taskHandler.AddCommandHandlerAsync<ValidateToken, bool>(ValidateTokenHandler); 
        }
        
        private async Task<bool> ValidateTokenHandler (Command<ValidateToken> command) {
            var validateToken   = command.Value;
            var clientId        = validateToken.clientId;
            var readCredentials = credentials.Read();
            var findCred        = readCredentials.Find(clientId);
            await Sync();
                
            UserCredential cred = findCred.Result;
            bool isValid = cred != null && cred.token == validateToken.token;
            return isValid;
        }
        
        public SendMessageTask<bool> ValidateTokenTask(string clientId, string token) {
            var command = new ValidateToken { clientId = clientId, token = token };
            return SendMessage<ValidateToken, bool>(command);
        }
    }

    public class UserRole : Entity {
        public  List<string> roles;
    }
    
    public class UserCredential : Entity {
        public  string      passwordHash;
        public  string      token;
        }
        
    public class ValidateToken {
        public          string  clientId;
        public          string  token;

        public override string  ToString() => clientId;
    }
}