// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Auth;
using Friflo.Json.Flow.Sync;

#if UNITY_5_3_OR_NEWER
    using ValueTask = System.Threading.Tasks.Task;
#endif

namespace Friflo.Json.Flow.UserAuth
{
    /// <summary>
    /// Control the access to a <see cref="UserDatabase"/> by "clientId" ("public" | "server") of a user.
    /// A "public" user is only able to <see cref="Authenticate"/> itself.
    /// A "server" user is able to read credentials and roles stored in a <see cref="UserDatabase"/>.
    /// </summary>
    public class UserDatabaseAuthenticator : Authenticator
    {
        private readonly Authorizer otherUser  = new AuthorizeDeny();
        private readonly Authorizer publicUser = new AuthorizeMessage(nameof(AuthenticateUser));
        private readonly Authorizer serverUser = new AuthorizeTaskType(TaskType.read);
        
#pragma warning disable 1998   // This async method lacks 'await' operators and will run synchronously. ....
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