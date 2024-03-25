// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    /// <summary>
    /// Control individual user access to database containers and commands. <br/>
    /// Each <b>user</b> has a set of <b>roles</b> stored in container <b>permissions</b>. <br/>
    /// Each <b>role</b> in container <b>roles</b> has a set of <b>rights</b> which grant or deny container access or command execution. 
    /// </summary>
    /// <remarks>
    /// <see cref="UserStore"/> can also be used as a non thread safe <see cref="IUserAuth"/> implementation.
    /// For a thread safe <see cref="IUserAuth"/> implementation use <see cref="UserAuth"/>.
    /// </remarks>
    public sealed class UserStore : FlioxClient, IUserAuth
    {
        // --- containers
        public   readonly   EntitySet <string, UserCredential> credentials;
        public   readonly   EntitySet <string, UserPermission> permissions;
        public   readonly   EntitySet <string, Role>           roles;
        public   readonly   EntitySet <string, UserTarget>     targets;
        
        public static class ID {
            /// <summary>user id in <see cref="UserStore.permissions"/> used for all users - authenticated and anonymous</summary>
            public  const string    AllUsers            = ".all-users";
            /// <summary>user id in <see cref="UserStore.permissions"/> used for authenticated users</summary>
            public  const string    AuthenticatedUsers  = ".authenticated-users";
            /// <summary>user id in <see cref="UserStore.permissions"/> used for Hub administrator</summary>
            public  const string    Admin               = "admin";
            /// <summary>role id in <see cref="UserStore.roles"/> used to enable full Hub access</summary>
            public  const string    HubAdmin            = "hub-admin";
            /// <summary>role id in <see cref="UserStore.roles"/> used to enable reading cluster database</summary>
            public  const string    ClusterInfo         = "cluster-info";
        }
        
        public UserStore(FlioxHub hub, string dbName = null) : base(hub, dbName) { }
        
        // --- commands
        /// <summary>authenticate user <see cref="Credentials"/>: <see cref="Credentials.userId"/> and <see cref="Credentials.token"/></summary>
        public CommandTask<AuthResult>           AuthenticateUser(Credentials command)  => send.Command<Credentials, AuthResult>(command);
        public CommandTask<ValidateUserDbResult> ValidateUserDb()                       => send.Command<ValidateUserDbResult>();
        public CommandTask<bool>                 ClearAuthCache()                       => send.Command<bool>();

        // --- IUserAuth
        public async Task<AuthResult> AuthenticateAsync(Credentials command) {
            var commandTask = AuthenticateUser(command);
            await SyncTasks().ConfigureAwait(false);
            return commandTask.Result;
        }
    }
}
