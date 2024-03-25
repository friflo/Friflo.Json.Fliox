// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.DB.UserAuth;

namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// Each <see cref="Role"/> has a set of <see cref="Role.taskRights"/>. <br/>
    /// Each <see cref="TaskRight"/> is a rule used to grant a specific database operation or command execution.<br/>
    /// The database operation or command execution is granted if any of it <see cref="Role.taskRights"/>
    /// grant access.
    /// </summary>
    [Discriminator("type", "right type")]
    [PolymorphType(typeof(DbFullRight),             "dbFull")]
    [PolymorphType(typeof(DbTaskRight),             "dbTask")]
    [PolymorphType(typeof(DbContainerRight),        "dbContainer")]
    [PolymorphType(typeof(SendMessageRight),        "sendMessage")]
    [PolymorphType(typeof(SubscribeMessageRight),   "subscribeMessage")]
    [PolymorphType(typeof(PredicateRight),          "predicate")]
    public abstract class TaskRight {
        /// <summary>optional description explaining the Right</summary>
        public              string      description;
        public    abstract  RightType   RightType { get; }

        public    abstract  TaskAuthorizer  ToAuthorizer();
        internal  abstract  void            Validate(in RoleValidation validation);
    }
}