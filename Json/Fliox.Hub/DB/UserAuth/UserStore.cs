// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;

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
    public sealed class UserStore : FlioxClient, IUserAuth
    {
        // --- containers
        public  readonly    EntitySet <JsonKey, UserCredential>  credentials;
        public  readonly    EntitySet <JsonKey, UserPermission>  permissions;
        public  readonly    EntitySet <string,  Role>            roles;
        
        /// <summary>"userId" used for a <see cref="UserStore"/> to perform user authentication.</summary>
        public const string Server              = "Server";
        /// <summary>"userId" used for a <see cref="UserStore"/> to request a user authentication with its token</summary>
        public const string AuthenticationUser  = "AuthenticationUser";
        
        public UserStore(FlioxHub hub, string database = null) : base(hub, database) { }
        
        // --- commands
        /// <summary>authenticate user <see cref="Credentials"/>: <see cref="Credentials.userId"/> and <see cref="Credentials.token"/></summary>
        public CommandTask<AuthResult> AuthenticateUser(Credentials command) {
            return SendCommand<Credentials, AuthResult>(nameof(AuthenticateUser), command);
        }
        public CommandTask<ValidateUserDbResult> ValidateUserDb() {
            return SendCommand<ValidateUserDbResult>(nameof(ValidateUserDb));
        }
        public CommandTask<bool> ClearAuthCache() {
            return SendCommand<bool>(nameof(ClearAuthCache));
        }
        
        public async Task<AuthResult> Authenticate(Credentials command) {
            var commandTask = AuthenticateUser(command);
            await SyncTasks().ConfigureAwait(false);
            return commandTask.Result;
        }
    }

    // -------------------------------------- models ---------------------------------------
    /// <summary>contains a <see cref="token"/> assigned to a user used for authentication</summary>
    public sealed class UserCredential {
        /// <summary>user name</summary>
        [Required]  public  JsonKey         id;
        /// <summary>user token</summary>
                    public  string          token;
                        
        public override string          ToString() => JsonSerializer.Serialize(this);
    }
    
    /// <summary>Set of <see cref="roles"/> assigned to a user used for authorization</summary>
    public sealed class UserPermission {
        /// <summary>user name</summary>
        [Required]  public  JsonKey         id;
        /// <summary>set of <see cref="roles"/> assigned to a user</summary>
        [Relation(nameof(UserStore.roles))]
                    public  List<string>    roles;

        public override string          ToString() => JsonSerializer.Serialize(this);
    }
    
    /// <summary>Contains a set of <see cref="rights"/> used for task authorization</summary>
    public sealed class Role {
        /// <summary><see cref="Role"/> name</summary>
        [Required]  public  string          id;
        /// <summary>a set of <see cref="rights"/> used for task authorization</summary>
        [Required]  public  List<Right>     rights;
        /// <summary>optional <see cref="description"/> explaining a <see cref="Role"/></summary>
                    public  string          description;
                        
        public override string          ToString() => JsonSerializer.Serialize(this);
    }
    
    // -------------------------------------- command models -------------------------------------
    /// <summary>user <see cref="Credentials"/> used for authentication</summary>
    public sealed class Credentials {
        [Required]  public  JsonKey         userId;
        [Required]  public  string          token;

        public override     string          ToString() => $"userId: {userId}";
    }
    
    /// <summary>Result of <see cref="UserStore.AuthenticateUser"/> command</summary>
    public sealed class AuthResult {
        /// <summary>true if authentication was successful</summary>
        public          bool            isValid;

        public override string          ToString() => $"isValid: {isValid}";
    }
    
    public sealed class ValidateUserDbResult {
        public          string[]        errors;
    }
    
}