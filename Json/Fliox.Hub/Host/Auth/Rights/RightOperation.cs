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
                        public  string                              database;
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
    
    public sealed class ContainerAccess
    {
        public          List<OperationType>     operations;
        public          List<Change>            subscribeChanges;
    }
    
    // ReSharper disable InconsistentNaming
    /// <summary>container operation type</summary>
    public enum OperationType {
        /// <summary>XXX</summary>
        create,
        upsert,
        delete,
        deleteAll,
        patch, 
        read,  
        query, 
        aggregate,
        mutate,
        full
    }
}