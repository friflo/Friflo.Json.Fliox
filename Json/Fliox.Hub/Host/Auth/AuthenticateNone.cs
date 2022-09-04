// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthenticateNone : Authenticator
    {
        public override Task Authenticate(SyncRequest syncRequest, SyncContext syncContext) {
            User user;
            ref var userId = ref syncRequest.userId;
            if (userId.IsNull()) {
                user = anonymousUser;
            } else {
                if (!users.TryGetValue(userId, out user)) {
                    user = new User(userId, null) {
                        TaskAuthorizer = AnonymousTaskAuthorizer, HubPermission = AnonymousHubPermission
                    };
                    users.TryAdd(userId, user);
                }
            }
            syncContext.AuthenticationFailed(user, "not authenticated", AnonymousTaskAuthorizer, AnonymousHubPermission);
            return Task.CompletedTask;
        }
    }
}
