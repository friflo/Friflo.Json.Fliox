// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;


namespace Friflo.Json.Fliox.Hub.Protocol
{
    // ----------------------------------- response -----------------------------------
    /// <summary>
    /// A <see cref="SyncResponse"/> is the response of <see cref="SyncRequest"/> executed by a <see cref="Host.FlioxHub"/>
    /// </summary>
    public sealed class SyncResponse : ProtocolResponse
    {
        /// <summary>for debugging - not used by Protocol</summary>
        [Serialize                                ("db")]
                    public  ShortString             database;
        /// <summary>list of task results corresponding to the <see cref="SyncRequest.tasks"/> in a <see cref="SyncRequest"/></summary>
                    public  ListOne<SyncTaskResult> tasks;

                    public  JsonValue               info;
        /// <summary>error message if authentication failed. null for successful authentication</summary>
                    public  string                  authError;
                        
        internal override   MessageType             MessageType => MessageType.resp;
        
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
                    tasks  = new ListOne<SyncTaskResult>(taskCapacity)
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
            response.info       = default;
            response.authError  = null;
            return response;
        }
    }
}