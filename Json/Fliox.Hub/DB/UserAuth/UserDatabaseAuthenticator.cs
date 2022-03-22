// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;


// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.DB.UserAuth
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
        public  readonly  Dictionary<JsonKey, IAuthorizer>  userRights = new Dictionary<JsonKey, IAuthorizer> (JsonKey.Equality) {
            { new JsonKey(UserStore.AuthenticationUser),    AuthUserRights },
            { new JsonKey(UserStore.Server),                ServerRights   },
        };
        
        private static readonly string DbName = null; // todo replace authorizer by instance members
            
        public static readonly    IAuthorizer   UnknownRights    = new AuthorizeDeny();
        public static readonly    IAuthorizer   AuthUserRights   = new AuthorizeAny(new IAuthorizer[] {
            new AuthorizeSendMessage(nameof(UserStore.AuthenticateUser), DbName),
            new AuthorizeContainer  (nameof(UserStore.permissions),  new []{OperationType.read}, DbName),
            new AuthorizeContainer  (nameof(UserStore.roles),        new []{OperationType.read, OperationType.query}, DbName),
        });
        public static readonly    IAuthorizer   ServerRights     = new AuthorizeAny(new IAuthorizer[] {
            new AuthorizeContainer(nameof(UserStore.credentials),  new []{OperationType.read}, DbName)
        });
        
        public UserDatabaseAuthenticator() : base (null) { }

        public override Task Authenticate(SyncRequest syncRequest, ExecuteContext executeContext) {
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

            if (userRights.TryGetValue(userId, out IAuthorizer rights)) {
                executeContext.AuthenticationSucceed(user, rights);
                return Task.CompletedTask;
            }
            // AuthenticationFailed() is not called to avoid giving a hint for a valid userId (user)
            executeContext.AuthenticationSucceed(user, UnknownRights);
            return Task.CompletedTask;
        }
    }
}