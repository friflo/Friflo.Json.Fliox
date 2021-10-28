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
    /// Authenticate users stored in the user database passed to <see cref="UserDatabaseAuthenticator(EntityDatabase)"/>.
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
            
        public static readonly    Authorizer   UnknownRights    = new AuthorizeDeny();
        public static readonly    Authorizer   AuthUserRights   = new AuthorizeAny(new Authorizer[] {
            new AuthorizeMessage(nameof(AuthenticateUser)),
            new AuthorizeContainer(nameof(UserStore.permissions),  new []{OperationType.read}),
            new AuthorizeContainer(nameof(UserStore.roles),        new []{OperationType.read, OperationType.query}),
        });
        public static readonly    Authorizer   ServerRights     = new AuthorizeAny(new Authorizer[] {
            new AuthorizeContainer(nameof(UserStore.credentials),  new []{OperationType.read})
        });
        
        public UserDatabaseAuthenticator(EntityDatabase userDatabase) : base (null) {
            userDatabase.handler.AddCommandHandlerAsync<AuthenticateUser, AuthenticateUserResult>(nameof(AuthenticateUser), AuthenticateUser);
        }
        
        private async Task<AuthenticateUserResult> AuthenticateUser (Command<AuthenticateUser> command) {
            using (var pooledStore = command.Pools.Pool(() => new UserStore(command.Hub, UserStore.Server)).Get()) {
                var store           = pooledStore.instance;
                var validateToken   = command.Value;
                var userId          = validateToken.userId;
                var readCredentials = store.credentials.Read();
                var findCred        = readCredentials.Find(userId);
                
                await store.SyncTasks().ConfigureAwait(false);

                UserCredential  cred    = findCred.Result;
                bool            isValid = cred != null && cred.token == validateToken.token;
                return new AuthenticateUserResult { isValid = isValid };
            }
        }
        
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
            // authState.SetFailed() is not called to avoid giving a hint for a valid userId (user)
            messageContext.AuthenticationSucceed(user, UnknownRights);
            return Task.CompletedTask;
        }
    }
}