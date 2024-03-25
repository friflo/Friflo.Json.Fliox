// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    // ---------------------------------- entity models ----------------------------------
    /// <summary>contains a <see cref="token"/> assigned to a user used for authentication</summary>
    public sealed class UserCredential {
        /// <summary>user id</summary>
        [Required]  public  string          id;
        /// <summary>user token</summary>
                    public  string          token;
                        
        public override     string          ToString() => JsonSerializer.Serialize(this);
    }
    
    /// <summary>Set of <see cref="roles"/> assigned to a user used for authorization</summary>
    public sealed class UserPermission {
        /// <summary>
        /// user id.<br/>
        /// id's with specific behavior:<br/>
        /// .all-users - specify permissions for authenticated and non authenticated users.<br/> 
        /// .authenticated-users - specify permissions for authenticated users.
        /// </summary>
        [Required]  public  string          id;
        /// <summary>set of <see cref="roles"/> assigned to a user</summary>
        [Relation(nameof(UserStore.roles))]
        [Required]  public  HashSet<string> roles;

        public override     string          ToString() => JsonSerializer.Serialize(this);
    }
    
    /// <summary>Contains a set of <see cref="taskRights"/> used for task authorization</summary>
    public sealed class Role {
        /// <summary><see cref="Role"/> name</summary>
        [Required]  public  string          id;
        /// <summary>a set of <see cref="taskRights"/> used for task authorization</summary>
        [Required]  public  List<TaskRight> taskRights;
        /// <summary>general request / connection rights for Hub access</summary>
                    public  HubRights       hubRights;
        /// <summary>optional <see cref="description"/> explaining a <see cref="Role"/></summary>
                    public  string          description;
                        
        public override     string          ToString() => JsonSerializer.Serialize(this);
    }
    
    /// <summary>
    /// contain the <see cref="groups"/> assigned to a user.<br/>
    /// These groups are used to enable forwarding of message events only to users of specific groups.
    /// </summary>
    public sealed class UserTarget {
        /// <summary>user id</summary>
        [Required]  public  string              id;
        /// <summary>list of <see cref="groups"/> assigned to a user</summary>
        [Required]  public  List<string>        groups;
                        
        public override     string              ToString() => JsonSerializer.Serialize(this);
    }
    
    // ---------------------------- command models - aka DTO's ---------------------------
    /// <summary>user <see cref="Credentials"/> used for authentication</summary>
    public sealed class Credentials {
        [Required]  public  string          userId;
        [Required]  public  string          token;

        public override     string          ToString() => $"userId: {userId}";
    }
    
    /// <summary>Result of <see cref="UserStore.AuthenticateUser"/> command</summary>
    public sealed class AuthResult {
        /// <summary>true if authentication was successful</summary>
                    public  bool            isValid;

        public override     string          ToString() => $"isValid: {isValid}";
    }
    
    public sealed class ValidateUserDbResult {
                    public  string[]        errors;
    }
}