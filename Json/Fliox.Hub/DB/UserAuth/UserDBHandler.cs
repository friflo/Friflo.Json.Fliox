// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    public class UserDBHandler : TaskHandler
    {
        public UserDBHandler() {
            AddCommandAsync<AuthenticateUser, AuthenticateUserResult> (nameof(AuthenticateUser), null, AuthenticateUser);
        }
        
        private async Task<AuthenticateUserResult> AuthenticateUser (Command<AuthenticateUser> command) {
            using(var pooled = command.Pool.Type(() => new UserStore(command.Hub)).Get()) {
                var store           = pooled.instance;
                store.UserId        = UserStore.Server;
                var validateToken   = command.Param;
                var userId          = validateToken.userId;
                var readCredentials = store.credentials.Read();
                var findCred        = readCredentials.Find(userId);
                
                await store.SyncTasks().ConfigureAwait(false);

                UserCredential  cred    = findCred.Result;
                bool            isValid = cred != null && cred.token == validateToken.token;
                return new AuthenticateUserResult { isValid = isValid };
            }
        }
    }
}