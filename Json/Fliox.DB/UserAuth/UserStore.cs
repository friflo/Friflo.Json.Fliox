// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth.Rights;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable UnassignedReadonlyField
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable CollectionNeverUpdated.Global
namespace Friflo.Json.Fliox.DB.UserAuth
{
    /// <summary>
    /// Provide access to an <see cref="EntityDatabase"/> storing user credentials ands roles.
    /// It can also be used as a non thread safe <see cref="IUserAuth"/> implementation.
    /// For a thread safe <see cref="IUserAuth"/> implementation use <see cref="UserAuth"/>.
    /// </summary>
    public class UserStore : EntityStore, IUserAuth
    {
        public  readonly    EntitySet <string, UserPermission>  permissions;
        public  readonly    EntitySet <string, UserCredential>  credentials;
        public  readonly    EntitySet <string, Role>            roles;
        
        /// <summary>"clientId" used for a <see cref="UserStore"/> to perform user authentication.</summary>
        public const string Server      = "Server";
        /// <summary>"clientId" used for a <see cref="UserStore"/> to request a user authentication with its token</summary>
        public const string AuthUser    = "AuthUser";
        
        public UserStore(EntityDatabase database, string clientId) : base(database, SyncTypeStore.Get(), clientId) {}
        
        public async Task<AuthenticateUserResult> AuthenticateUser(AuthenticateUser command) {
            var commandTask = SendMessage<AuthenticateUser, AuthenticateUserResult>(command);
            await Sync().ConfigureAwait(false);
            return commandTask.Result;
        }
    }

    // -------------------------------------- models ---------------------------------------
    public class UserPermission {
        [Fri.Required]  public  string          id;
                        public  List<string>    roles;

        public override         string ToString() => JsonDebug.ToJson(this, false);
    }
    
    public class UserCredential {
        [Fri.Required]  public  string          id;
                        public  string          passHash;
                        public  string          token;
                        
        public override         string ToString() => JsonDebug.ToJson(this, false);
    }
    
    public class Role {
        [Fri.Required]  public  string          id;
        [Fri.Required]  public  List<Right>     rights;
                        public  string          description;
                        
        public override         string ToString() => JsonDebug.ToJson(this, false);
    }
    
    // -------------------------------------- commands -------------------------------------
    public class AuthenticateUser {
        public          string  clientId;
        public          string  token;

        public override string  ToString() => clientId;
    }
    
    public class AuthenticateUserResult {
        public          bool            isValid;
    }
}