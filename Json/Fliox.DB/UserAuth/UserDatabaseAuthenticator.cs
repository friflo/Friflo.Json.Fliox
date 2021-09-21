// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Auth.Rights;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.DB.Protocol;


// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.DB.UserAuth
{
    /// <summary>
    /// Control the access to a <see cref="UserDatabaseHandler"/> by "clientId" (<see cref="UserStore.AuthUser"/> |
    /// <see cref="UserStore.Server"/>) of a user.
    /// <br></br>
    /// A <see cref="UserStore.AuthUser"/> user is only able to <see cref="Authenticate"/> itself.
    /// A <see cref="UserStore.Server"/> user is able to read credentials and roles stored in a <see cref="UserDatabaseHandler"/>.
    /// </summary>
    public class UserDatabaseAuthenticator : Authenticator
    {
        public  readonly    Dictionary<string, Authorizer>  userRights = new Dictionary<string, Authorizer> {
            { UserStore.AuthUser,   AuthUserRights },
            { UserStore.Server,     ServerRights   },
        };
            
        public static readonly    Authorizer   UnknownRights    = new AuthorizeDeny();
        public static readonly    Authorizer   AuthUserRights   = new AuthorizeAny(new Authorizer[] {
            new AuthorizeMessage(nameof(AuthenticateUser)),
            new AuthorizeContainer(nameof(UserStore.permissions),  new []{OperationType.read}),
            new AuthorizeContainer(nameof(UserStore.roles),        new []{OperationType.read, OperationType.query}),
        });
        public static readonly    Authorizer   ServerRights     = new AuthorizeAny(new Authorizer[] {
            new AuthorizeContainer(nameof(UserStore.credentials),  new []{OperationType.read})
        });
        
        public override Task Authenticate(SyncRequest syncRequest, MessageContext messageContext) {
            var clientId = syncRequest.clientId;
            if (userRights.TryGetValue(clientId, out Authorizer rights)) {
                messageContext.authState.SetSuccess(rights);
                return Task.CompletedTask;
            }
            // authState.SetFailed() is not called to avoid giving a hint for a valid clientId (user)
            messageContext.authState.SetSuccess(UnknownRights);
            return Task.CompletedTask;
        }
    }
}