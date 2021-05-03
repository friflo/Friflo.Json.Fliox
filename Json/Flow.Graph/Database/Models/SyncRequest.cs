// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Database.Models
{
    // ------------------------------ SyncRequest / SyncResponse ------------------------------
    public class SyncRequest
    {
        public  List<DatabaseTask>                      tasks;
    }
    
    public class SyncResponse
    {
        public  List<TaskResult>                        tasks;
        public  Dictionary<string, ContainerEntities>   results;
        
        internal ContainerEntities GetContainerResult(string container) {
            if (results.TryGetValue(container, out ContainerEntities result))
                return result;
            result = new ContainerEntities {
                container = container,
                entities = new Dictionary<string,EntityValue>()
            };
            results.Add(container, result);
            return result;
        }
    }
    
    // ------ ContainerEntities
    public class ContainerEntities
    {
        public  string                                  container; // only for debugging
        public  Dictionary<string, EntityValue>         entities;
        
        internal void AddEntities(Dictionary<string, EntityValue> add) {
            foreach (var entity in add) {
                entities.TryAdd(entity.Key, entity.Value);
            }
        }
    }
}
