// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.DB.UserAuth;

namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// Each <see cref="Role"/> has a set of <see cref="Role.taskRights"/>. <br/>
    /// Each <see cref="TaskRight"/> is a rule used to grant or deny a specific database operation or command execution.<br/>
    /// The database operation or command execution is granted if any of it <see cref="Role.taskRights"/>
    /// grant access.
    /// </summary>
    [Discriminator("type", Description = "right type")]
    [PolymorphType(typeof(DbFullRight),             Discriminant = "dbFull")]
    [PolymorphType(typeof(DbTaskRight),             Discriminant = "dbTask")]
    [PolymorphType(typeof(DbContainerRight),        Discriminant = "dbContainer")]
    [PolymorphType(typeof(SendMessageRight),        Discriminant = "sendMessage")]
    [PolymorphType(typeof(SubscribeMessageRight),   Discriminant = "subscribeMessage")]
    [PolymorphType(typeof(PredicateRight),          Discriminant = "predicate")]
    public abstract class TaskRight {
        /// <summary>optional description explaining the Right</summary>
        public              string      description;
        public    abstract  RightType   RightType { get; }

        public    abstract  TaskAuthorizer  ToAuthorizer();
        internal  abstract  void            Validate(in RoleValidation validation);
    }
}