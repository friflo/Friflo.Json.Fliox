// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Read entities by id from the specified <see cref="container"/> using given list of <see cref="ids"/><br/>
    /// To return also entities referenced by entities listed in <see cref="ids"/> use <see cref="references"/>. <br/>
    /// This mimic the functionality of a <b>LEFT JOIN</b> in <b>SQL</b>
    /// </summary>
    public sealed class ReadEntities : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Required]  public  string              container;
        /// <summary> name of the primary key property of the returned entities </summary>
                    public  string              keyName;
                    public  bool?               isIntKey;
        /// <summary> list of requested entity <see cref="ids"/> </summary>
        [Required]  public  List<JsonKey>       ids;
        /// <summary> used to request the entities referenced by properties of a read task result </summary>
                    public  List<References>    references;
        
        public   override   TaskType            TaskType => TaskType.read;
        public   override   string              TaskName =>  $"container: '{container}'";

        public override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (container == null)
                return MissingContainer();

            if (ids == null)
                return MissingField(nameof(ids));
            foreach (var id in ids) {
                if (id.IsNull())
                    return InvalidTask("elements in ids must not be null");
            }
            if (!ValidReferences(references, out var error))
                return error;

            var entityContainer = database.GetOrCreateContainer(container);
            var result          = await entityContainer.ReadEntities(this, syncContext).ConfigureAwait(false);
            
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            var entities = result.entities;
            
            if (references != null && references.Count > 0) {
                var readRefResults =
                    await entityContainer.ReadReferences(references, entities, entityContainer.name, "", response, syncContext).ConfigureAwait(false);
                // returned readRefResults.references is always set. Each references[] item contain either a result or an error.
                result.references = readRefResults.references;
            }
            // entities elements can be updated in ReadReferences()
            var containerResult = response.GetContainerResult(container);
            containerResult.AddEntities(entities);
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="ReadEntities"/> task
    /// </summary>
    public sealed class ReadEntitiesResult : SyncTaskResult
    {
                    public  List<ReferencesResult>  references;
        [Ignore]    public  EntityValue[]           entities;
        
        [Ignore]    public  CommandError            Error { get; set; }
        internal override   TaskType                TaskType => TaskType.read;
        
        /// <summary>
        /// Validate all <see cref="EntityValue.value"/>'s in the result set.
        /// Validation is required for all <see cref="EntityContainer"/> implementations which cannot ensure that the
        /// <see cref="EntityValue.Json"/> value of <see cref="entities"/> is valid JSON.
        /// 
        /// E.g. <see cref="FileContainer"/> cannot ensure this, as the file content can be written
        /// or modified from external processes - for example by manually changing its JSON content with an editor.
        /// 
        /// A <see cref="MemoryContainer"/> does not require validation as its key/values are always written via
        /// Fliox.Hub.Client library - which generate valid JSON.
        /// 
        /// So database adapters which can ensure the JSON value is always valid made calling <see cref="ValidateEntities"/>
        /// obsolete - like Postgres/JSONB, Azure Cosmos DB or MongoDB.
        /// </summary>
        public void ValidateEntities(string container, string keyName, SyncContext syncContext) {
            using (var pooled = syncContext.EntityProcessor.Get()) {
                EntityProcessor processor = pooled.instance;
                for (int n = 0; n < entities.Length; n++) {
                    var entity = entities[n];
                    if (entity.Error != null) {
                        continue;
                    }
                    var json    = entity.Json;
                    if (json.IsNull()) {
                        continue;
                    }
                    keyName = keyName ?? "id";
                    if (processor.Validate(json, keyName, out JsonKey payloadId, out string error)) {
                        if (entity.key.IsEqual(payloadId))
                            continue;
                        error = $"entity key mismatch. '{keyName}': '{payloadId.AsString()}'";
                    }
                    var entityError = new EntityError {
                        type        = EntityErrorType.ParseError,
                        message     = error,
                        id          = entity.key,
                        container   = container
                    };
                    // entity.SetError(entity.key, entityError); - used when using class EntityValue
                    entities[n] = new EntityValue(entity.key, entityError);
                }
            }
        }
    }
}