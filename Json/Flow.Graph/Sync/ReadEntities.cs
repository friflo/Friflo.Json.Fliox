// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public class ReadEntities
    {
        public  HashSet<string>                 ids;
        public  List<References>                references;
    }
    
    /// The data of requested entities are added to <see cref="ContainerEntities.entities"/> 
    public class ReadEntitiesResult: ICommandResult
    {
        public  List<ReferencesResult>          references;
        public  CommandError                    Error { get; set; }
        [Fri.Ignore]
        public  bool                            entitiesValidated;

        [Fri.Ignore]
        public  Dictionary<string,EntityValue>  entities;
        
        internal void ValidateEntities(string container, SyncContext syncContext) {
            if (entitiesValidated)
                return;
            using (var pooledValidator = syncContext.pools.JsonValidator.Get()) {
                var validator = pooledValidator.instance;
                foreach (var entityEntry in entities) {
                    var entity = entityEntry.Value;
                    if (entity.Error != null) {
                        continue;
                    }
                    var json = entity.Json;
                    if (json != null && !validator.ValidJson(json, out string error)) {
                        var entityError = new EntityError {
                            type        = EntityErrorType.ParseError,
                            message     = error,
                            id          = entityEntry.Key,
                            container   = container
                        };
                        entity.SetError(entityError);
                    }
                }
                entitiesValidated = true;
            }
        }
    }
}