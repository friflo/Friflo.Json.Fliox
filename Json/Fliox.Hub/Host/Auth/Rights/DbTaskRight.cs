// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// <see cref="DbTaskRight"/> grant <see cref="database"/> access by a set of task <see cref="types"/>. <br/> 
    /// </summary>
    public sealed class DbTaskRight : TaskRight
    {
        /// <summary>a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*'</summary>
        [Required]  public      string          database;
        /// <summary>set fo task types like: create, read, upsert, delete, query, ...</summary>
        [Required]  public      List<TaskType>  types;
        
        public  override        RightType       RightType => RightType.dbTask;
        
        public override TaskAuthorizer ToAuthorizer() {
            var databaseName = database;
            if (types.Count == 1) {
                return GetAuthorizer(databaseName, types[0]);
            }
            var list = new List<TaskAuthorizer>(types.Count);
            foreach (var task in types) {
                list.Add(GetAuthorizer(databaseName, task));
            }
            return new AuthorizeAny(list);
        }
        
        internal override void Validate(in RoleValidation validation) {
            validation.ValidateDatabase(this, database);
        }

        
        private static TaskAuthorizer GetAuthorizer(string database, TaskType taskType) {
            switch (taskType) {
                case TaskType.read:                return new AuthorizeTaskType(TaskType.read,              database);
                case TaskType.query:               return new AuthorizeTaskType(TaskType.query,             database);
                case TaskType.aggregate:           return new AuthorizeTaskType(TaskType.aggregate,         database);
                case TaskType.create:              return new AuthorizeTaskType(TaskType.create,            database);
                case TaskType.upsert:              return new AuthorizeTaskType(TaskType.upsert,            database);
                case TaskType.merge:               return new AuthorizeTaskType(TaskType.merge,             database);
                case TaskType.delete:              return new AuthorizeTaskType(TaskType.delete,            database);
                case TaskType.closeCursors:        return new AuthorizeTaskType(TaskType.closeCursors,      database);
                //
                case TaskType.message:             return new AuthorizeTaskType(TaskType.message,           database);
                case TaskType.command:             return new AuthorizeTaskType(TaskType.command,           database);
                case TaskType.subscribeChanges:    return new AuthorizeTaskType(TaskType.subscribeChanges,  database);
                case TaskType.subscribeMessage:    return new AuthorizeTaskType(TaskType.subscribeMessage,  database);
            }
            throw new InvalidOperationException($"unknown authorization taskType: {taskType}");
        }

    }
}