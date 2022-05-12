// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthenticateNone : Authenticator
    {
        private readonly Authorizer anonymousAuthorizer;
        

        public AuthenticateNone(Authorizer anonymousAuthorizer)
            : base (anonymousAuthorizer)
        {
            this.anonymousAuthorizer = anonymousAuthorizer ?? throw new NullReferenceException(nameof(anonymousAuthorizer));
        }
        
        public override Task Authenticate(SyncRequest syncRequest, ExecuteContext executeContext) {
            User user;
            ref var userId = ref syncRequest.userId;
            if (userId.IsNull()) {
                user = anonymousUser;
            } else {
                if (!users.TryGetValue(userId, out user)) {
                    user = new User(userId, null, anonymousAuthorizer);
                    users.TryAdd(userId, user);
                }
            }
            executeContext.AuthenticationFailed(user, "not authenticated", anonymousAuthorizer);
            return Task.CompletedTask;
        }
    }
}
