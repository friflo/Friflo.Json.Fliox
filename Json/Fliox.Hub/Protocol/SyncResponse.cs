// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;

namespace Friflo.Json.Fliox.Hub.Protocol
{
    // ----------------------------------- response -----------------------------------
    /// <summary>
    /// A <see cref="SyncResponse"/> is the response of <see cref="SyncRequest"/> executed by a <see cref="Host.FlioxHub"/>
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
                    public  JsonValue                               info;
                        
        internal override   MessageType                             MessageType => MessageType.resp;
        
        public ContainerEntities FindContainer(string containerName) {
            if (containers == null)
                return null;
            foreach (var container in containers) {
                if (container.container == containerName)
                    return container;
            }
            return null;
        }
        
        internal ContainerEntities GetContainerResult(string containerName) {
            if (containers == null) containers = new List<ContainerEntities>();
            foreach (var container in containers) {
                if (container.container == containerName)
                    return container;
            }
            var result = new ContainerEntities { container = containerName };
            containers.Add(result);
            return result;
        }

        internal static void AddEntityErrors(ref List<EntityError> target, List<EntityError> errors) {
            if (errors == null)
                return;
            if (target == null) {
                target = new List<EntityError>(errors.Count);
            }
            foreach (var error in errors) {
                target.Add(error);
            }
        }
        
        [Conditional("DEBUG")]
        [ExcludeFromCodeCoverage]
        public void AssertResponse(SyncRequest request) {
            var expect = request.tasks.Count;
            var actual = tasks.Count;
            if (expect != actual) {
                var msg = $"Expect response.task.Count == request.task.Count: expect: {expect}, actual: {actual}"; 
                throw new InvalidOperationException(msg);
            }
        }
        
        internal static SyncResponse Create(SyncContext syncContext, int taskCapacity) {
            var syncPools = syncContext.syncPools;
            if (syncPools == null) {
                return new SyncResponse {
                    tasks  = new List<SyncTaskResult>(taskCapacity)
                };
            }
            var tasks           = syncPools.taskResultsPool.Create();
            tasks.Clear();
            var response        = syncPools.responsePool.Create();
            // --- ProtocolResponse
            response.reqId      = null;
            response.clientId   = default;
            // --- SyncResponse
            response.tasks      = tasks;
            response.containers = null;
            response.info       = default;
            return response;
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
        /// Required only by <see cref="RemoteHost"/> for serialization
        [Serialize                            ("cont")]
        [Required]  public  string              container;
        /// <summary>number of <see cref="entities"/> - not utilized by Protocol</summary>
        [DebugInfo] public  int?                count;
        /// <summary>
        /// all <see cref="entities"/> from the <see cref="container"/> resulting from
        /// <see cref="ReadEntities"/> and <see cref="QueryEntities"/> tasks of a <see cref="SyncRequest"/>
        /// </summary>
        /// Required only by <see cref="RemoteHost"/> for serialization
        [Serialize                            ("set")]
        [Required]  public  List<JsonValue>     entities;
        /// <summary>list of entities not found by <see cref="ReadEntities"/> tasks</summary>
        /// Required only by <see cref="RemoteHost"/> for serialization
                    public  List<JsonKey>       notFound;
        /// <summary>list of entity errors read from <see cref="container"/></summary>
        /// Required only by <see cref="RemoteHost"/> for serialization
                    public  List<EntityError>   errors;
        
        [Ignore]    public readonly Dictionary<JsonKey, EntityValue>    entityMap = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality);

        public override     string              ToString() => container;

        internal void AddEntities(EntityValue[] add) {
            entityMap.EnsureCapacity(entityMap.Count + add.Length);
            foreach (var entity in add) {
                entityMap.TryAdd(entity.key, entity);
            }
        }
    }
}