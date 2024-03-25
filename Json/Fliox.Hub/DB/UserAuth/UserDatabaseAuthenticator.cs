// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static Friflo.Json.Fliox.Hub.Host.Auth.Rights.OperationType;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    public static class UserDB
    {
        public static readonly DatabaseSchema Schema = DatabaseSchema.CreateFromType(typeof(UserStore));
        
        public static class ID {
            /// <summary>"userId" used for a <see cref="UserStore"/> to perform user authentication.</summary>
            public const string Server              = "Server";
            /// <summary>"userId" used for a <see cref="UserStore"/> to request a user authentication with its token</summary>
            public const string AuthenticationUser  = "AuthenticationUser";
        }
    }
    
    /// <summary>
    /// Authenticate users stored in the user database.
    /// </summary>
    /// <remarks>
    /// If user authentication succeed it returns also the roles attached to a user to enable authorization for each task.
    /// The schema of the user database is defined in <see cref="UserStore"/>.
    /// <br/>
    /// The access to the user database itself requires also authentication by one of the predefined users:
    /// <see cref="UserDB.ID.AuthenticationUser"/> or <see cref="UserDB.ID.Server"/>.
    /// <br></br>
    /// A <see cref="UserDB.ID.AuthenticationUser"/> user is only able to <see cref="AuthenticateAsync"/> itself.
    /// A <see cref="UserDB.ID.Server"/> user is able to read credentials and roles stored in a user database.
    /// </remarks>
    public sealed class UserDatabaseAuthenticator : Authenticator
    {
        private  readonly   Dictionary<ShortString, TaskAuthorizer> userRights;
        private  readonly   User                                    anonymous;

        public UserDatabaseAuthenticator(string userDbName) {
            var changes         = new [] { EntityChange.create, EntityChange.upsert, EntityChange.delete, EntityChange.merge };
            var authUserRights  = new AuthorizeAny(new TaskAuthorizer[] {
                new AuthorizeSendMessage     (nameof(UserStore.AuthenticateUser), userDbName),
                new AuthorizeContainer       (nameof(UserStore.permissions), new []{ read, query },  userDbName),
                new AuthorizeContainer       (nameof(UserStore.roles),       new []{ read, query },  userDbName),
                new AuthorizeContainer       (nameof(UserStore.targets),     new []{ read, upsert }, userDbName),
                new AuthorizeSubscribeChanges(nameof(UserStore.credentials), changes, userDbName),
                new AuthorizeSubscribeChanges(nameof(UserStore.permissions), changes, userDbName),
                new AuthorizeSubscribeChanges(nameof(UserStore.roles),       changes, userDbName),
                new AuthorizeSubscribeChanges(nameof(UserStore.targets),     changes, userDbName)
            });
            var serverRights    = new AuthorizeAny(new TaskAuthorizer[] {
                new AuthorizeContainer       (nameof(UserStore.credentials), new []{ read, create },   userDbName),
                new AuthorizeContainer       (nameof(UserStore.permissions), new []{ upsert, create, read }, userDbName),
                new AuthorizeContainer       (nameof(UserStore.roles),       new []{ upsert, create }, userDbName)
            });
            userRights = new Dictionary<ShortString, TaskAuthorizer> (ShortString.Equality) {
                { new ShortString(UserDB.ID.AuthenticationUser),    authUserRights },
                { new ShortString(UserDB.ID.Server),                serverRights   },
            };
            anonymous   = new User(User.AnonymousId).Set(default, TaskAuthorizer.None, HubPermission.None, null);
        }

        public override Task AuthenticateAsync(SyncRequest syncRequest, SyncContext syncContext) {
            Authenticate(syncRequest, syncContext);
            return Task.CompletedTask;
        }
        
        public override bool IsSynchronous(SyncRequest syncRequest) => true;
        
        public override void Authenticate(SyncRequest syncRequest, SyncContext syncContext) {
            ref var userId = ref syncRequest.userId;
            User user;
            if (userId.IsNull()) {
                user = anonymous;
            } else {
                if (!users.TryGetValue(userId, out  user)) {
                    user = new User(userId).Set(default, TaskAuthorizer.None, HubPermission.None, null);
                    users.TryAdd(userId, user);
                }
            }
            if (userRights.TryGetValue(userId, out TaskAuthorizer taskAuthorizer)) {
                syncContext.AuthenticationSucceed(user, taskAuthorizer, anonymous.hubPermission);
                return;
            }
            // AuthenticationFailed() is not called to avoid giving a hint for a valid userId (user)
            syncContext.AuthenticationSucceed(user, anonymous.taskAuthorizer, anonymous.hubPermission);
        }
    }
}