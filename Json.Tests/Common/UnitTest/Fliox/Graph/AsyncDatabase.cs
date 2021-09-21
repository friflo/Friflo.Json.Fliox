// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph
{
    /// <summary>
    /// An <see cref="EntityDatabase"/> implementation which execute the continuation of <see cref="ExecuteSync"/>
    /// never synchronously to test <see cref="EntityStore.Sync"/> running not synchronously.
    /// </summary>
    public class AsyncDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  local;

        public AsyncDatabase(EntityDatabase local) {
            this.local = local;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return local.GetOrCreateContainer(name);
        }
        
        public override async Task<Response<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            const bool originalContext = true;
            // force release the thread back to the caller so continuation will not be executed synchronously.
            await Task.Delay(1).ConfigureAwait(originalContext);
            var response = await local.ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
            return response;
        }
    }
}
