// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Auth.Rights;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.UserAuth
{
    /// <summary>
    /// Provide access to an <see cref="EntityDatabase"/> storing user credentials ands roles.
    /// It can also be used as a non thread safe <see cref="IUserAuth"/> implementation.
    /// For a thread safe <see cref="IUserAuth"/> implementation use <see cref="UserAuth"/>.
    /// </summary>
    public class UserStore : EntityStore, IUserAuth
    {
        /// <summary>"clientId" used for a <see cref="UserStore"/> to perform user authentication.</summary>
        public const string Server      = "Server";
        /// <summary>"clientId" used for a <see cref="UserStore"/> to request a user authentication with its token</summary>
        public const string AuthUser    = "AuthUser";
        
        public readonly EntitySet<UserPermission>   permissions;
        public readonly EntitySet<UserCredential>   credentials;
        public readonly EntitySet<Role>             roles;
        
        public UserStore(EntityDatabase database, string clientId) : base(database, SyncTypeStore.Get(), clientId) {
            permissions = new EntitySet<UserPermission> (this);
            credentials = new EntitySet<UserCredential> (this);
            roles       = new EntitySet<Role>           (this);
        }
        
        public async Task<AuthenticateUserResult> AuthenticateUser(AuthenticateUser command) {
            var commandTask = SendMessage<AuthenticateUser, AuthenticateUserResult>(command);
            await Sync();
            return commandTask.Result;
        }
    }

    // -------------------------------------- models ---------------------------------------
    public class UserPermission : Entity {
        public  List<string>    roles;
    }
    
    public class UserCredential : Entity {
        public  string          passHash;
        public  string          token;
    }
    
    public class Role : Entity {
        [Fri.Property(Required = true)]
        public  List<Right>     rights;
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