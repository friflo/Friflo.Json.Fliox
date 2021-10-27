// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs
{
    public class NoopDatabaseHub : FlioxHub
    {
        internal NoopDatabaseHub (string hostName = null) : base(null, hostName) { }
                
        public override Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            var result = new SyncResponse {
                tasks       = new List<SyncTaskResult>(),
                resultMap   = new Dictionary<string, ContainerEntities>()
            };
            var response = new ExecuteSyncResult(result);
            return Task.FromResult(response);
        }
    }
}