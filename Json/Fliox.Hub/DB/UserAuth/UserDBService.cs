// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    public sealed class UserDBService : DatabaseService
    {
        private     FlioxHub                userHub;
        private     ObjectPool<UserStore>   storePool;

        public UserDBService() {
            AddCommandHandlerAsync<Credentials, AuthResult>         (nameof(AuthenticateUser),  AuthenticateUser);
            AddCommandHandlerAsync<JsonValue, ValidateUserDbResult> (nameof(ValidateUserDb),    ValidateUserDb);
            AddCommandHandler<JsonValue, bool>                      (nameof(ClearAuthCache),    ClearAuthCache);
        }
        
        internal void Init(FlioxHub userHub) {
            if (this.userHub != null) throw new InvalidOperationException($"{nameof(UserDBService)} already initialized");
            this.userHub    = userHub;
            storePool       = new ObjectPool<UserStore>(() => new UserStore(userHub));
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
        
        private static readonly    JsonKey      AdminId     = new JsonKey(UserStore.ID.Admin);
        private static readonly    JsonKey      HubAdminId  = new JsonKey(UserStore.ID.HubAdmin);
        
        private static bool ValidateTask(SyncRequestTask task, out TaskErrorResult error) {
            if (task.ContainsEntityChange(Change.delete, new ShortString(nameof(UserStore.credentials)), AdminId)) {
                error = new TaskErrorResult (TaskErrorType.PermissionDenied, $"credentials '{AdminId}' must not be deleted");
                return false;
            }
            if (task.ContainsEntityChange(Change.All, new ShortString(nameof(UserStore.permissions)), AdminId)) {
                error = new TaskErrorResult (TaskErrorType.PermissionDenied, $"permission '{AdminId}' must not be changed");
                return false;
            }
            if (task.ContainsEntityChange(Change.All, new ShortString(nameof(UserStore.roles)), HubAdminId)) {
                error = new TaskErrorResult (TaskErrorType.PermissionDenied, $"role '{HubAdminId}' must not be changed");
                return false;
            }
            error = null;
            return true;
        }
        
        private async Task<Result<AuthResult>> AuthenticateUser (Param<Credentials> param, MessageContext context)
        {
            using (var pooled = storePool.Get()) {
                var store       = pooled.instance;
                store.UserId    = UserDB.ID.Server;
                if (!param.GetValidate(out var authenticate, out var error)) {
                    return Result.ValidationError(error);
                }
                var userId          = authenticate.userId;
                var readCredentials = store.credentials.Read();
                var findCred        = readCredentials.Find(userId);
                
                await store.TrySyncTasks().ConfigureAwait(false);
                
                if (!readCredentials.Success) {
                    return Result.Error(readCredentials.Error.Message);
                }

                UserCredential  cred    = findCred.Result;
                bool            isValid = cred != null && cred.token == authenticate.token;
                return new AuthResult { isValid = isValid };
            }
        }
        
        private async Task<Result<ValidateUserDbResult>> ValidateUserDb (Param<JsonValue> param, MessageContext context) {
            var authenticator   = (UserAuthenticator)context.Hub.Authenticator;
            var databases       = context.Hub.GetDatabases().Keys.ToHashSet();
            var errors          = await authenticator.ValidateUserDb(databases).ConfigureAwait(false);
            
            return new ValidateUserDbResult { errors = errors.ToArray() };
        }
        
        private static Result<bool> ClearAuthCache (Param<JsonValue> param, MessageContext context) {
            var authenticator   = (UserAuthenticator)context.Hub.Authenticator;
            authenticator.users.Clear();
            authenticator.roleCache.Clear();
            return true;
        }
    }
}