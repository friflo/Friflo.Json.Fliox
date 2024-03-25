// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs
{
    /// <summary>
    /// A <see cref="FlioxHub"/> implementation which execute the continuation of <see cref="ExecuteRequestAsync"/>
    /// never synchronously to test <see cref="FlioxClient.SyncTasks"/> running not synchronously.
    /// </summary>
    public class AsyncDatabaseHub : FlioxHub
    {
        private readonly    FlioxHub  local;

        public AsyncDatabaseHub(EntityDatabase database, SharedEnv env) : base(database, env) {
            local = new FlioxHub(database, env);
        }
        
        public override async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext) {
            const bool originalContext = true;
            // force release the thread back to the caller so continuation will not be executed synchronously.
            await Task.Delay(1).ConfigureAwait(originalContext);
            var response = await local.ExecuteRequestAsync(syncRequest, syncContext).ConfigureAwait(false);
            return response;
        }
    }
}
