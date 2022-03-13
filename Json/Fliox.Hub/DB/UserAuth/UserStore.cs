// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;
using Friflo.Json.Fliox.Mapper;

using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;

// ReSharper disable UnassignedReadonlyField
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable CollectionNeverUpdated.Global
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
    public class UserStore : FlioxClient, IUserAuth
    {
        // --- containers
        public  readonly    EntitySet <JsonKey, UserCredential>  credentials;
        public  readonly    EntitySet <JsonKey, UserPermission>  permissions;
        public  readonly    EntitySet <string,  Role>            roles;
        
        /// <summary>"userId" used for a <see cref="UserStore"/> to perform user authentication.</summary>
        public const string Server              = "Server";
        /// <summary>"userId" used for a <see cref="UserStore"/> to request a user authentication with its token</summary>
        public const string AuthenticationUser  = "AuthenticationUser";
        
        public UserStore(FlioxHub hub) : base(hub) { }
        
        // --- commands
        public CommandTask<AuthResult> AuthenticateUser(Credentials command) {
            return SendCommand<Credentials, AuthResult>(nameof(AuthenticateUser), command);
        }
        
        public async Task<AuthResult> Authenticate(Credentials command) {
            var commandTask = AuthenticateUser(command);
            await SyncTasks().ConfigureAwait(false);
            return commandTask.Result;
        }
    }

    // -------------------------------------- models ---------------------------------------
    public class UserPermission {
        [Req]   public  JsonKey         id;
        [Fri.Relation(nameof(UserStore.roles))]
                public  List<string>    roles;

        public override string          ToString() => JsonSerializer.Serialize(this);
    }
    
    public class UserCredential {
        [Req]   public  JsonKey         id;
                public  string          token;
                        
        public override string          ToString() => JsonSerializer.Serialize(this);
    }
    
    public class Role {
        [Req]   public  string          id;
        [Req]   public  List<Right>     rights;
                public  string          description;
                        
        public override string          ToString() => JsonSerializer.Serialize(this);
    }
    
    // -------------------------------------- command models -------------------------------------
    public class Credentials {
        [Req]   public  JsonKey         userId;
        [Req]   public  string          token;

        public override string          ToString() => $"userId: {userId}";
    }
    
    public class AuthResult {
                public bool             isValid;

        public override string          ToString() => $"isValid: {isValid}";
    }
}