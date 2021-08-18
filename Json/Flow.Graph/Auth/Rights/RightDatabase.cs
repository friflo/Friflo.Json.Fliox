// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Flow.Auth.Rights
{
    public class RightDatabase : Right
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
    
    public class ContainerAccess
    {
        public          List<OperationType>     operations;
        public          List<Change>            subscribeChanges;
    }
    
    // ReSharper disable InconsistentNaming
    public enum OperationType {
        create,
        update,
        delete,
        patch, 
        read,  
        query, 
        mutate,
        full
    }
}