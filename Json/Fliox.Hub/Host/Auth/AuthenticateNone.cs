// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    /// <summary>
    /// Used as default <see cref="Authenticator"/> in a <see cref="FlioxHub"/>.<br/>
    /// All users accessing a <see cref="FlioxHub"/> are not authenticated - <see cref="SyncContext.Authenticated"/> = false <br/>
    /// Execution of all user tasks are authorized. The <see cref="User.taskAuthorizer"/> and <see cref="User.hubPermission"/>
    /// of <see cref="SyncContext.User"/> grant full access.
    /// </summary>
    public sealed class AuthenticateNone : Authenticator
    {
        public override Task AuthenticateAsync(SyncRequest syncRequest, SyncContext syncContext) {
            Authenticate(syncRequest, syncContext);
            return Task.CompletedTask;
        }
        
        public override bool IsSynchronous(SyncRequest syncRequest) => true;
        
        public override void Authenticate(SyncRequest syncRequest, SyncContext syncContext) {
            User user;
            ref var userId = ref syncRequest.userId;
            if (userId.IsNull()) {
                user = anonymousUser;
            } else {
                if (!users.TryGetValue(userId, out user)) {
                    user = new User(userId, default) {
                        taskAuthorizer = AnonymousTaskAuthorizer, hubPermission = AnonymousHubPermission
                    };
                    users.TryAdd(userId, user);
                }
            }
            syncContext.AuthenticationFailed(user, "not authenticated", AnonymousTaskAuthorizer, AnonymousHubPermission);
        }
    }
}
