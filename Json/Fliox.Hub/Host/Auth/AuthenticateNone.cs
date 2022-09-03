// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthenticateNone : Authenticator
    {
        private readonly    Authorizer      anonymousAuthorizer;
        private readonly    HubPermission   anonymousHubPermission;
        

        public AuthenticateNone(Authorizer anonymousAuthorizer, HubPermission anonymousHubPermission)
            : base (anonymousAuthorizer, anonymousHubPermission)
        {
            this.anonymousAuthorizer    = anonymousAuthorizer       ?? throw new ArgumentNullException(nameof(anonymousAuthorizer));
            this.anonymousHubPermission = anonymousHubPermission    ?? throw new ArgumentNullException(nameof(anonymousHubPermission));
        }
        
        public override Task Authenticate(SyncRequest syncRequest, SyncContext syncContext) {
            User user;
            ref var userId = ref syncRequest.userId;
            if (userId.IsNull()) {
                user = anonymousUser;
            } else {
                if (!users.TryGetValue(userId, out user)) {
                    user = new User(userId, null, anonymousAuthorizer, anonymousHubPermission);
                    users.TryAdd(userId, user);
                }
            }
            syncContext.AuthenticationFailed(user, "not authenticated", anonymousAuthorizer, anonymousHubPermission);
            return Task.CompletedTask;
        }
    }
}
