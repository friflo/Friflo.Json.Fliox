// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

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
        private readonly        Dictionary<JsonKey, Authorizer> userRights;
        private static readonly Authorizer                      UnknownRights    = new AuthorizeDeny();
        
        public UserDatabaseAuthenticator(string userDbName) : base (null) {
            var changes         = new [] { Change.create, Change.upsert, Change.delete, Change.patch };
            var authUserRights  = new AuthorizeAny(new Authorizer[] {
                new AuthorizeSendMessage(nameof(UserStore.AuthenticateUser), userDbName),
                new AuthorizeContainer  (nameof(UserStore.permissions),  new []{ OperationType.read, OperationType.query }, userDbName),
                new AuthorizeContainer  (nameof(UserStore.roles),        new []{ OperationType.read, OperationType.query }, userDbName),
                new AuthorizeSubscribeChanges (nameof(UserStore.permissions),   changes, userDbName),
                new AuthorizeSubscribeChanges (nameof(UserStore.roles),         changes, userDbName)
            });
            var serverRights    = new AuthorizeAny(new Authorizer[] {
                new AuthorizeContainer  (nameof(UserStore.credentials),  new []{ OperationType.read }, userDbName)
            });
            userRights = new Dictionary<JsonKey, Authorizer> (JsonKey.Equality) {
                { new JsonKey(UserStore.AuthenticationUser),    authUserRights },
                { new JsonKey(UserStore.Server),                serverRights   },
            };
        }

        public override Task Authenticate(SyncRequest syncRequest, SyncContext syncContext) {
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
                syncContext.AuthenticationSucceed(user, rights);
                return Task.CompletedTask;
            }
            // AuthenticationFailed() is not called to avoid giving a hint for a valid userId (user)
            syncContext.AuthenticationSucceed(user, UnknownRights);
            return Task.CompletedTask;
        }
    }
}