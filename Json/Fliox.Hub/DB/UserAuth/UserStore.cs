// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Auth.Rights;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable UnassignedReadonlyField
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable CollectionNeverUpdated.Global
namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    /// <summary>
    /// Provide access to a <see cref="FlioxHub"/> storing user credentials ands roles.
    /// It can also be used as a non thread safe <see cref="IUserAuth"/> implementation.
    /// For a thread safe <see cref="IUserAuth"/> implementation use <see cref="UserAuth"/>.
    /// </summary>
    public class UserStore : FlioxClient, IUserAuth
    {
        public  readonly    EntitySet <JsonKey, UserCredential>  credentials;
        public  readonly    EntitySet <JsonKey, UserPermission>  permissions;
        public  readonly    EntitySet <string,  Role>            roles;
        
        /// <summary>"userId" used for a <see cref="UserStore"/> to perform user authentication.</summary>
        public const string Server              = "Server";
        /// <summary>"userId" used for a <see cref="UserStore"/> to request a user authentication with its token</summary>
        public const string AuthenticationUser  = "AuthenticationUser";
        
        public UserStore(FlioxHub hub) : base(hub) { }
        
        public CommandTask<AuthenticateUserResult> AuthenticateUser(AuthenticateUser command) {
            return SendCommand<AuthenticateUser, AuthenticateUserResult>(nameof(AuthenticateUser), command);
        }
        
        public async Task<AuthenticateUserResult> Authenticate(AuthenticateUser command) {
            var commandTask = AuthenticateUser(command);
            await SyncTasks().ConfigureAwait(false);
            return commandTask.Result;
        }
    }

    // -------------------------------------- models ---------------------------------------
    public class UserPermission {
        [Fri.Required]  public  JsonKey         id;
                        public  List<string>    roles;

        public override         string ToString() => JsonSerializer.Serialize(this);
    }
    
    public class UserCredential {
        [Fri.Required]  public  JsonKey         id;
                        public  string          passHash;
                        public  string          token;
                        
        public override         string ToString() => JsonSerializer.Serialize(this);
    }
    
    public class Role {
        [Fri.Required]  public  string          id;
        [Fri.Required]  public  List<Right>     rights;
                        public  string          description;
                        
        public override         string ToString() => JsonSerializer.Serialize(this);
    }
    
    // -------------------------------------- commands -------------------------------------
    public class AuthenticateUser {
        [Fri.Required]  public  JsonKey userId;
        [Fri.Required]  public  string  token;

        public override string  ToString() => $"userId: {userId}";
    }
    
    public class AuthenticateUserResult {
        public          bool            isValid;

        public override string ToString() => $"isValid: {isValid}";
    }
}