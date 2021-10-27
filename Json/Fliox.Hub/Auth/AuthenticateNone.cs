// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Auth
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
            User user;
            ref var userId = ref syncRequest.userId;
            if (userId.IsNull()) {
                user = anonymousUser;
            } else {
                if (!users.TryGetValue(userId, out user)) {
                    user = new User(userId, null, unknown);
                    users.TryAdd(userId, user);
                }
            }
            messageContext.AuthenticationFailed(user, "not authenticated", unknown);
            return Task.CompletedTask;
        }
    }
}
