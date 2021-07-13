// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Auth;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.UserAuth
{
    public class ValidationAuthenticator : Authenticator
    {
        private readonly Authorizer otherUser  = new AuthorizeDeny();
        private readonly Authorizer publicUser = new AuthorizeMessage(nameof(ValidateToken));
        private readonly Authorizer serverUser = new AuthorizeTaskType(TaskType.read);
        
        public override async ValueTask Authenticate(SyncRequest syncRequest, MessageContext messageContext) {
            var clientId = syncRequest.clientId;
            switch (clientId) {
                case "public": 
                    messageContext.authState.SetSuccess(publicUser);
                    break;
                case "server": 
                    // todo validate with secret
                    messageContext.authState.SetSuccess(serverUser);
                    break;
                default:
                    messageContext.authState.SetSuccess(otherUser);
                    break;
            }
        }
    }
}