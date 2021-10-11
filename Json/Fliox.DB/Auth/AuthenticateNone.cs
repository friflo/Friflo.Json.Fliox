// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Auth
{
    public sealed class AuthenticateNone : Authenticator
    {
        private readonly Authorizer unknown;
        

        public AuthenticateNone(Authorizer unknown)
            : base (unknown)
        {
            this.unknown = unknown ?? throw new NullReferenceException(nameof(unknown));
        }
        
        public override Task Authenticate(SyncRequest syncRequest, MessageContext messageContext) {
            AuthUser authUser;
            ref var userId = ref syncRequest.userId;
            if (userId.IsNull()) {
                authUser = anonymousUser;
            } else {
                if (!authUsers.TryGetValue(userId, out authUser)) {
                    authUser = new AuthUser(userId, null, unknown);
                    authUsers.TryAdd(userId, authUser);
                }
            }
            messageContext.authState.SetFailed(authUser, "not authenticated", unknown);
            return Task.CompletedTask;
        }
    }
}
