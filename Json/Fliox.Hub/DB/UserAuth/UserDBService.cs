// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    public sealed class UserDBService : DatabaseService
    {
        public UserDBService() {
            AddCommandHandlerAsync<Credentials, AuthResult>         (nameof(AuthenticateUser),  AuthenticateUser);
            AddCommandHandlerAsync<JsonValue, ValidateUserDbResult> (nameof(ValidateUserDb),    ValidateUserDb);
            AddCommandHandler<JsonValue, bool>                      (nameof(ClearAuthCache),    ClearAuthCache);
        }
        
        public override async Task<SyncTaskResult> ExecuteTaskAsync (SyncRequestTask task, EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (!AuthorizeTask(task, syncContext, out var error)) {
                return error;
            }
            if (syncContext.hub.Authenticator is UserAuthenticator) {
                if (!ValidateTask(task, out var validationError)) {
                    return validationError;
                }
            }
            return await task.ExecuteAsync(database, response, syncContext).ConfigureAwait(false);
        }
        
        private static readonly    JsonKey      AdminId     = new JsonKey(UserAuthenticator.AdminId);
        private static readonly    JsonKey      HubAdminId  = new JsonKey(UserAuthenticator.HubAdminId);
        
        private static bool ValidateTask(SyncRequestTask task, out TaskErrorResult error) {
            if (task.ContainsEntityChange(Change.delete, new ShortString(nameof(UserStore.credentials)), AdminId)) {
                error = new TaskErrorResult (TaskErrorResultType.PermissionDenied, $"credentials '{AdminId}' must not be deleted");
                return false;
            }
            if (task.ContainsEntityChange(Change.All, new ShortString(nameof(UserStore.permissions)), AdminId)) {
                error = new TaskErrorResult (TaskErrorResultType.PermissionDenied, $"permission '{AdminId}' must not be changed");
                return false;
            }
            if (task.ContainsEntityChange(Change.All, new ShortString(nameof(UserStore.roles)), HubAdminId)) {
                error = new TaskErrorResult (TaskErrorResultType.PermissionDenied, $"role '{HubAdminId}' must not be changed");
                return false;
            }
            error = null;
            return true;
        }
        
        private async Task<AuthResult> AuthenticateUser (Param<Credentials> param, MessageContext command) {
            using(var pooled = command.Pool.Type(() => new UserStore(command.Hub)).Get()) {
                var store           = pooled.instance;
                store.UserId        = UserStore.Server;
                if (!param.GetValidate(out var authenticate, out var error)) {
                    command.Error(error);
                    return null;
                }
                var userId          = authenticate.userId;
                var readCredentials = store.credentials.Read();
                var findCred        = readCredentials.Find(userId);
                
                await store.TrySyncTasks().ConfigureAwait(false);
                
                if (!readCredentials.Success) {
                    command.Error(readCredentials.Error.Message);
                    return null;  
                }

                UserCredential  cred    = findCred.Result;
                bool            isValid = cred != null && cred.token == authenticate.token;
                return new AuthResult { isValid = isValid };
            }
        }
        
        private async Task<ValidateUserDbResult> ValidateUserDb (Param<JsonValue> param, MessageContext command) {
            var authenticator   = (UserAuthenticator)command.Hub.Authenticator;
            var databases       = command.Hub.GetDatabases().Keys.ToHashSet();
            var errors          = await authenticator.ValidateUserDb(databases).ConfigureAwait(false);
            
            return new ValidateUserDbResult { errors = errors.ToArray() };
        }
        
        private static bool ClearAuthCache (Param<JsonValue> param, MessageContext command) {
            var authenticator   = (UserAuthenticator)command.Hub.Authenticator;
            authenticator.users.Clear();
            authenticator.roleCache.Clear();
            return true;
        }
    }
}