// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// <see cref="RightOperation"/> grant <see cref="database"/> access for the given <see cref="containers"/>
    /// based on a set of <see cref="ContainerAccess.operations"/>. <br/>
    /// E.g. create, read, upsert, delete, query or aggregate (count)<br/>
    /// It also allows subscribing database changes by <see cref="ContainerAccess.subscribeChanges"/>
    /// </summary>
    public sealed class RightOperation : Right
    {
        /// <summary>a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*'</summary>
                        public  string                              database;
        /// <summary>grant execution of operations and subscriptions on listed <see cref="containers"/> </summary>
        [Fri.Required]  public  Dictionary<string, ContainerAccess> containers;
        public  override        RightType                           RightType => RightType.operation;
        
        public override Authorizer ToAuthorizer() {
            var list = new List<Authorizer>(containers.Count);
            foreach (var pair in containers) {
                var name        = pair.Key;
                var container   = pair.Value;
                var access      = container.operations;
                if (access != null && access.Count > 0) {
                    list.Add(new AuthorizeContainer(name, access, database));
                }
                var subscribeChanges   = container.subscribeChanges;
                if (subscribeChanges != null && subscribeChanges.Count > 0) {
                    list.Add(new AuthorizeSubscribeChanges(name, subscribeChanges, database));
                }
            }
            return new AuthorizeAny(list);
        }
    }
    
    /// <summary>Grant execution of specific container operations and subscriptions</summary>
    public sealed class ContainerAccess
    {
        /// <summary>Set of granted operation types</summary>
        public          List<OperationType>     operations;
        /// <summary>Set of granted change subscriptions</summary>
        public          List<Change>            subscribeChanges;
    }
    
    // ReSharper disable InconsistentNaming
    /// <summary>Use to allow specific container operations in <see cref="ContainerAccess"/></summary>
    public enum OperationType {
        /// <summary>allow to create entities in a container</summary>
        create,
        /// <summary>allow to upsert entities in a container</summary>
        upsert,
        /// <summary>allow to delete entities in a container</summary>
        delete,
        /// <summary>allow to delete all container entities</summary>
        deleteAll,
        /// <summary>allow to patch entities in a container</summary>
        patch,
        /// <summary>allow to read entities in a container</summary>
        read,
        /// <summary>allow to query entities in a container</summary>
        query,
        /// <summary>allow to aggregate - count - entities in a container</summary>
        aggregate,
        /// <summary>allow to mutate - create, upsert, delete and patch - entities in a container</summary>
        mutate,
        /// <summary>allow all operation types in a container</summary>
        full
    }
}