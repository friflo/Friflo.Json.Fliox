// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Auth;
using Friflo.Json.Fliox.Hub.Auth.Rights;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;


// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.UserAuth
{
    /// <summary>
    /// Authenticate users stored in the user database.
    /// If user authentication succeed it returns also the roles attached to a user to enable authorization for each task.
    /// The schema of the user database is defined in <see cref="UserStore"/>.
    /// <br/>
    /// The access to the user database itself requires also authentication by one of the predefined users:
    /// <see cref="UserStore.AuthenticationUser"/> or <see cref="UserStore.Server"/>.
    /// <br></br>
    /// A <see cref="UserStore.AuthenticationUser"/> user is only able to <see cref="Authenticate"/> itself.
    /// A <see cref="UserStore.Server"/> user is able to read credentials and roles stored in a user database.
    /// </summary>
    public class UserDatabaseAuthenticator : Authenticator
    {
        public  readonly    Dictionary<JsonKey, Authorizer>  userRights = new Dictionary<JsonKey, Authorizer> (JsonKey.Equality) {
            { new JsonKey(UserStore.AuthenticationUser),    AuthUserRights },
            { new JsonKey(UserStore.Server),                ServerRights   },
        };
        
        private static readonly   string DatabaseName = null; // todo convert static Authorizer to instance member and set database name
            
        public static readonly    Authorizer   UnknownRights    = new AuthorizeDeny(DatabaseName);
        public static readonly    Authorizer   AuthUserRights   = new AuthorizeAny(new Authorizer[] {
            new AuthorizeMessage(nameof(AuthenticateUser), DatabaseName),
            new AuthorizeContainer(nameof(UserStore.permissions),  new []{OperationType.read}, DatabaseName),
            new AuthorizeContainer(nameof(UserStore.roles),        new []{OperationType.read, OperationType.query}, DatabaseName),
        }, DatabaseName);
        public static readonly    Authorizer   ServerRights     = new AuthorizeAny(new Authorizer[] {
            new AuthorizeContainer(nameof(UserStore.credentials),  new []{OperationType.read}, DatabaseName)
        }, DatabaseName);
        
        public UserDatabaseAuthenticator() : base (null) { }

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
                messageContext.AuthenticationSucceed(user, rights);
                return Task.CompletedTask;
            }
            // AuthenticationFailed() is not called to avoid giving a hint for a valid userId (user)
            messageContext.AuthenticationSucceed(user, UnknownRights);
            return Task.CompletedTask;
        }
    }
}