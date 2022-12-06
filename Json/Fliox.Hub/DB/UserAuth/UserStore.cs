// Copyright (c) Ullrich Praetz. All rights reserved.
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
        public   readonly   EntitySet <JsonKey, UserCredential> credentials;
        public   readonly   EntitySet <JsonKey, UserPermission> permissions;
        public   readonly   EntitySet <string,  Role>           roles;
        public   readonly   EntitySet <JsonKey, UserTarget>     targets;
        
        /// <summary>"userId" used for a <see cref="UserStore"/> to perform user authentication.</summary>
        public const string Server              = "Server";
        /// <summary>"userId" used for a <see cref="UserStore"/> to request a user authentication with its token</summary>
        public const string AuthenticationUser  = "AuthenticationUser";
        
        public UserStore(FlioxHub hub, string dbName = null) : base(hub, dbName) { }
        
        // --- commands
        /// <summary>authenticate user <see cref="Credentials"/>: <see cref="Credentials.userId"/> and <see cref="Credentials.token"/></summary>
        public CommandTask<AuthResult>           AuthenticateUser(Credentials command)  => SendCommand<Credentials, AuthResult>(nameof(AuthenticateUser), command);
        public CommandTask<ValidateUserDbResult> ValidateUserDb()                       => SendCommand<ValidateUserDbResult>(nameof(ValidateUserDb));
        public CommandTask<bool>                 ClearAuthCache()                       => SendCommand<bool>(nameof(ClearAuthCache));

        // --- IUserAuth
        public async Task<AuthResult> AuthenticateAsync(Credentials command) {
            var commandTask = AuthenticateUser(command);
            await SyncTasks().ConfigureAwait(false);
            return commandTask.Result;
        }
    }
}
