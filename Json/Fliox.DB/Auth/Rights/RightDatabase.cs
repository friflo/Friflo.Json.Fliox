// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.DB.Auth.Rights
{
    public sealed class RightDatabase : Right
    {
        [Fri.Required]  public  Dictionary<string, ContainerAccess> containers;
        public  override        RightType                           RightType => RightType.database;
        
        public override Authorizer ToAuthorizer() {
            var list = new List<Authorizer>(containers.Count);
            foreach (var pair in containers) {
                var name        = pair.Key;
                var container   = pair.Value;
                var access      = container.operations;
                if (access != null && access.Count > 0) {
                    list.Add(new AuthorizeContainer(name, access));
                }
                var subscribeChanges   = container.subscribeChanges;
                if (subscribeChanges != null && subscribeChanges.Count > 0) {
                    list.Add(new AuthorizeSubscribeChanges(name, subscribeChanges));
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
    public enum OperationType {
        create,
        upsert,
        delete,
        deleteAll,
        patch, 
        read,  
        query, 
        mutate,
        full
    }
}