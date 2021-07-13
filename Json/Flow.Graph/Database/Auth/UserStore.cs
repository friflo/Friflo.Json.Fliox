// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Auth
{
    public class UserStore : EntityStore
    {
        public readonly EntitySet<UserRole>         roles;
        public readonly EntitySet<UserCredential>   credentials;
        
        public UserStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            roles       = new EntitySet<UserRole>(this);
            credentials = new EntitySet<UserCredential>(this);
            database.taskHandler = new UserTaskHandler(this);
        }
        
        public SendMessageTask<bool> ValidateTokenTask(string clientId, string token) {
            var command = new ValidateToken { clientId = clientId, token = token };
            return SendMessage<ValidateToken, bool>(command);
        }
    }
    
    class UserTaskHandler : TaskHandler {
        readonly UserStore userStore;
        
        public UserTaskHandler (UserStore userStore) {
            this.userStore = userStore;
        }

        public override async Task<TaskResult> ExecuteTask (DatabaseTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            // todo add typed TaskHandler.AddMessageHandler(string name)
            if (task is SendMessage message && message.name == nameof(ValidateToken)) {
                using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                    var reader = pooledMapper.instance.reader;
                    var validateToken   = reader.Read<ValidateToken>(message.value.json);
                    var clientId        = validateToken.clientId;
                    var readCredentials = userStore.credentials.Read();
                    var findCred        = readCredentials.Find(clientId);
                    await userStore.Sync();
                
                    UserCredential  cred = findCred.Result;
                    bool isValid = cred != null && cred.token == validateToken.token;
                    var value = new JsonValue { json = isValid ? "true" : "false" };
                    TaskResult result = new SendMessageResult { result = value };
                    return result;
                }
            }
            return await base.ExecuteTask(task, database, response, messageContext);
        }
    }
    
    public class UserRole : Entity {
        public List<string> roles;
    }
    
    public class UserCredential : Entity {
        public string       passwordHash;
        public string       token;
    }
    
    public class ValidateToken {
        public string   clientId;
        public string   token;
    }
}