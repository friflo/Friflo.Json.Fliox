// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// Each <see cref="DB.UserAuth.Role"/> has a set of <see cref="DB.UserAuth.Role.rights"/>. <br/>
    /// Each <see cref="Right"/> is a rule used to grant or deny a specific database operation or command execution.<br/>
    /// The database operation or command execution is granted if any of it <see cref="DB.UserAuth.Role.rights"/>
    /// grant access.
    /// </summary>
    [Discriminator("type", Description = "right type")]
    [Polymorph(typeof(AllowRight),              Discriminant = "allow")]
    [Polymorph(typeof(TaskRight),               Discriminant = "task")]
    [Polymorph(typeof(SendMessageRight),        Discriminant = "sendMessage")]
    [Polymorph(typeof(SubscribeMessageRight),   Discriminant = "subscribeMessage")]
    [Polymorph(typeof(OperationRight),          Discriminant = "operation")]
    [Polymorph(typeof(PredicateRight),          Discriminant = "predicate")]
    public abstract class Right {
        /// <summary>optional description explaining the Right</summary>
        public              string      description;
        public    abstract  RightType   RightType { get; }

        public    abstract  Authorizer  ToAuthorizer();
        internal  abstract  void        Validate(in RoleValidation validation);
    }
}