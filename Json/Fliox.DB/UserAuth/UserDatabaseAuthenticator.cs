// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Auth.Rights;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;


// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.DB.UserAuth
{
    /// <summary>
    /// Control the access to a <see cref="UserDatabaseHandler"/> by "userId" (<see cref="UserStore.AuthenticationUser"/> |
    /// <see cref="UserStore.Server"/>) of a user.
    /// <br></br>
    /// A <see cref="UserStore.AuthenticationUser"/> user is only able to <see cref="Authenticate"/> itself.
    /// A <see cref="UserStore.Server"/> user is able to read credentials and roles stored in a <see cref="UserDatabaseHandler"/>.
    /// </summary>
    public class UserDatabaseAuthenticator : Authenticator
    {
        public  readonly    Dictionary<JsonKey, Authorizer>  userRights = new Dictionary<JsonKey, Authorizer> (JsonKey.Equality) {
            { new JsonKey(UserStore.AuthenticationUser),    AuthUserRights },
            { new JsonKey(UserStore.Server),                ServerRights   },
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
        
        public UserDatabaseAuthenticator()
            : base (null)
        { }
        
        public override Task Authenticate(SyncRequest syncRequest, MessageContext messageContext) {
            ref var userId = ref syncRequest.userId;
            User user;
            if (userId.IsNull()) {
                user = anonymousUser;
            } else {
                if (!users.TryGetValue(userId, out  user)) {
                    user = new User(userId, null, null);
                    users.TryAdd(userId, user);
                }
            }

            if (userRights.TryGetValue(userId, out Authorizer rights)) {
                messageContext.authState.SetSuccess(user, rights);
                return Task.CompletedTask;
            }
            // authState.SetFailed() is not called to avoid giving a hint for a valid userId (user)
            messageContext.authState.SetSuccess(user, UnknownRights);
            return Task.CompletedTask;
        }
    }
}