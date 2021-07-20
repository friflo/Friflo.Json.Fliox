// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- sub task -----------------------------------
    public class ReadEntities
    {
        [Fri.Property(Required = true)]
        public  HashSet<string>                 ids;
        public  List<References>                references;
    }
    
    // ----------------------------------- sub task result -----------------------------------
    /// The data of requested entities are added to <see cref="ContainerEntities.entities"/> 
    public class ReadEntitiesResult: ICommandResult
    {
        [Fri.Property(Required = true)]
        public  List<ReferencesResult>          references;
        public  CommandError                    Error { get; set; }

        [Fri.Ignore]
        public  Dictionary<string,EntityValue>  entities;
        
        /// <summary>
        /// Validate all <see cref="EntityValue.value"/>'s in the result set.
        /// Validation is required for all <see cref="EntityContainer"/> implementations which cannot ensure that the
        /// <see cref="EntityValue.Json"/> value of <see cref="entities"/> is valid JSON.
        /// 
        /// E.g. <see cref="FileContainer"/> cannot ensure this, as the file content can be written
        /// or modified from extern processes - for example by manually changing its JSON content with an editor.
        /// 
        /// A <see cref="MemoryContainer"/> does not require validation as its key/values are always written via
        /// Database/Graph library - which generate valid JSON.
        /// 
        /// So database adapters which can ensure the JSON value is always valid made calling <see cref="ValidateEntities"/>
        /// obsolete - like Postgres/JSONB, Azure Cosmos DB or MongoDB.
        /// </summary>
        public void ValidateEntities(string container, MessageContext messageContext) {
            using (var pooledValidator = messageContext.pools.EntityValidator.Get()) {
                var validator = pooledValidator.instance;
                foreach (var entityEntry in entities) {
                    var entity = entityEntry.Value;
                    if (entity.Error != null) {
                        continue;
                    }
                    var id      = entityEntry.Key;
                    var json    = entity.Json;
                    if (json != null && !validator.IsValidEntity(json, id, out string error)) {
                        var entityError = new EntityError {
                            type        = EntityErrorType.ParseError,
                            message     = error,
                            id          = entityEntry.Key,
                            container   = container
                        };
                        entity.SetError(entityError);
                    }
                }
            }
        }
    }
}