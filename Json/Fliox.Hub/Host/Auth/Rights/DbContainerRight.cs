// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// <see cref="DbContainerRight"/> grant <see cref="database"/> access for the given <see cref="containers"/>
    /// based on a set of <see cref="ContainerAccess.operations"/>. <br/>
    /// E.g. create, read, upsert, delete, query or aggregate (count)<br/>
    /// It also allows subscribing database changes by <see cref="ContainerAccess.subscribeChanges"/>
    /// </summary>
    public sealed class DbContainerRight : TaskRight
    {
        /// <summary>a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*'</summary>
        [Required]  public              string                  database;
        /// <summary>grant execution of operations and subscriptions on listed <see cref="containers"/> </summary>
        [Required]  public              List<ContainerAccess>   containers;
                    public  override    RightType               RightType => RightType.dbContainer;
        
        public override TaskAuthorizer ToAuthorizer() {
            var databaseName = database;
            var list = new List<TaskAuthorizer>(containers.Count);
            foreach (var container in containers) {
                var name        = container.name;
                var access      = container.operations;
                if (access != null && access.Count > 0) {
                    list.Add(new AuthorizeContainer(name, access, databaseName));
                }
                var subscribeChanges   = container.subscribeChanges;
                if (subscribeChanges != null && subscribeChanges.Count > 0) {
                    list.Add(new AuthorizeSubscribeChanges(name, subscribeChanges, databaseName));
                }
            }
            return new AuthorizeAny(list);
        }
        
        internal override void Validate(in RoleValidation validation) {
            validation.ValidateDatabase(this, database);
        }
    }
    
    /// <summary>Grant execution of specific container operations and subscriptions</summary>
    public sealed class ContainerAccess
    {
        /// <summary>Container name</summary>
        [Required]  public  string              name;
        /// <summary>Set of granted operation types</summary>
                    public  List<OperationType> operations;
        /// <summary>Set of granted change subscriptions</summary>
                    public  List<EntityChange>  subscribeChanges;

        public override     string              ToString() => name; 
    }
    
    // ReSharper disable InconsistentNaming
    /// <summary>Use to allow specific container operations in <see cref="ContainerAccess"/></summary>
    public enum OperationType {
        /// <summary>allow to create entities in a container</summary>
        create      = 1,
        /// <summary>allow to upsert entities in a container</summary>
        upsert      = 2,
        /// <summary>allow to delete entities in a container</summary>
        delete      = 3,
        /// <summary>allow to delete all container entities</summary>
        deleteAll   = 4,
        /// <summary>allow to patch entities in a container</summary>
        merge       = 5,
        /// <summary>allow to read entities in a container</summary>
        read        = 6,
        /// <summary>allow to query entities in a container</summary>
        query       = 7,
        /// <summary>allow to aggregate - count - entities in a container</summary>
        aggregate   = 8,
        /// <summary>allow to mutate - create, upsert, delete and patch - entities in a container</summary>
        mutate      = 9,
        /// <summary>allow all operation types in a container</summary>
        full        = 10
    }
}