// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Protocol
{
    // ----------------------------------- response -----------------------------------
    /// <summary>
    /// The response send back from a host in reply of a <see cref="SyncRequest"/>
    /// </summary>
    public sealed class SyncResponse : ProtocolResponse
    {
        /// <summary>for debugging - not used by Protocol</summary>
                        public  string                                  database;
        /// <summary>list of task results corresponding to the <see cref="SyncRequest.tasks"/> in a <see cref="SyncRequest"/></summary>
                        public  List<SyncTaskResult>                    tasks;
        /// <summary>entities as results from the <see cref="SyncRequest.tasks"/> in a <see cref="SyncRequest"/>
        /// grouped by container</summary>
                        public  List<ContainerEntities>                 containers;
        // key of all Dictionary's is the container name
        [Fri.Ignore]    public  Dictionary<string, ContainerEntities>   resultMap;
        /// <summary>errors caused by <see cref="CreateEntities"/> tasks grouped by container</summary>
                        public  Dictionary<string, EntityErrors>        createErrors; // lazy instantiation
        /// <summary>errors caused by <see cref="UpsertEntities"/> tasks grouped by container</summary>
                        public  Dictionary<string, EntityErrors>        upsertErrors; // lazy instantiation
        /// <summary>errors caused by <see cref="PatchEntities"/> tasks grouped by container</summary>
                        public  Dictionary<string, EntityErrors>        patchErrors;  // lazy instantiation
        /// <summary>errors caused by <see cref="DeleteEntities"/> tasks grouped by container</summary>
                        public  Dictionary<string, EntityErrors>        deleteErrors; // lazy instantiation
        /// <summary>optional JSON value to return debug / development data - e.g. execution times or resource usage.</summary> 
                        public  JsonValue                               info;
                        
        internal override       MessageType                             MessageType => MessageType.resp;
        
        internal ContainerEntities GetContainerResult(string container) {
            if (resultMap.TryGetValue(container, out ContainerEntities result))
                return result;
            result = new ContainerEntities {
                container = container,
                entityMap = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality)
            };
            resultMap.Add(container, result);
            return result;
        }
        
        internal static EntityErrors GetEntityErrors(ref Dictionary<string, EntityErrors> entityErrorMap, string container) {
            if (entityErrorMap == null) {
                entityErrorMap = new Dictionary<string, EntityErrors>();
            }
            if (entityErrorMap.TryGetValue(container, out EntityErrors result))
                return result;
            result = new EntityErrors(container);
            entityErrorMap.Add(container, result);
            return result;
        }
        
        [Conditional("DEBUG")]
        public void AssertResponse(SyncRequest request) {
            var expect = request.tasks.Count;
            var actual = tasks.Count;
            if (expect != actual) {
                var msg = $"Expect response.task.Count == request.task.Count: expect: {expect}, actual: {actual}"; 
                throw new InvalidOperationException(msg);
            }
        }
    }
    
    // ----------------------------------- sync results -----------------------------------
    /// <summary>
    /// Used by <see cref="SyncResponse"/> to return the <see cref="entities"/> as results
    /// from <see cref="SyncRequest.tasks"/> of a <see cref="SyncRequest"/>
    /// </summary>
    public sealed class ContainerEntities
    {
        /// <summary>container name the of the returned <see cref="entities"/> </summary>
        /// Required only by <see cref="RemoteHostHub"/> for serialization
        [Fri.Required]  public  string                              container;
        /// <summary>number of <see cref="entities"/> - not utilized by Protocol</summary>
        [DebugInfo]     public  int?                                count;
        /// <summary>all <see cref="entities"/> as results from <see cref="SyncRequest.tasks"/> of a <see cref="SyncRequest"/></summary>
        /// Required only by <see cref="RemoteHostHub"/> for serialization
        [Fri.Required]  public  List<JsonValue>                     entities;
        /// <summary>list of entities not found by <see cref="ReadEntities"/> tasks</summary>
        /// Required only by <see cref="RemoteHostHub"/> for serialization
                        public  List<JsonKey>                       notFound;
        /// <summary>list of errors when accessing entities from a database</summary>
        /// Required only by <see cref="RemoteHostHub"/> for serialization
                        public  Dictionary<JsonKey, EntityError>    errors    = new Dictionary<JsonKey, EntityError>(JsonKey.Equality); // todo should be instantiated only if required
        
        [Fri.Ignore]    public  Dictionary<JsonKey, EntityValue>    entityMap = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality);

        public override         string                              ToString() => container;

        internal void AddEntities(Dictionary<JsonKey, EntityValue> add) {
            entityMap.EnsureCapacity(entityMap.Count + add.Count);
            foreach (var entity in add) {
                entityMap.TryAdd(entity.Key, entity.Value);
            }
        }
    }
    
    public sealed class EntityErrors
    {
        [DebugInfo]     public  string                              container;
        [Fri.Required]  public  Dictionary<JsonKey, EntityError>    errors = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
        
        public EntityErrors() {} // required for TypeMapper

        public EntityErrors(string container) {
            this.container  = container;
            errors          = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
        }
        
        internal void AddErrors(Dictionary<JsonKey, EntityError> errors) {
            foreach (var error in errors) {
                this.errors.TryAdd(error.Key, error.Value);
            }
        }

        internal void SetInferredErrorFields() {
            foreach (var errorEntry in errors) {
                var error = errorEntry.Value;
                // error .id & .container are not serialized as they are redundant data.
                // Infer their values from containing errors dictionary
                error.id        = errorEntry.Key;
                error.container = container;
            }
        }
    }
}