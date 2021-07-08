// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    /// <summary>
    /// An <see cref="EntityDatabase"/> implementation which execute the continuation of <see cref="ExecuteSync"/>
    /// never synchronously.
    /// </summary>
    public class AsyncDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  local;

        public AsyncDatabase(EntityDatabase local) {
            this.local = local;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            if (TryGetContainer(name, out EntityContainer container)) {
                return container;
            }
            EntityContainer localContainer = local.GetOrCreateContainer(name);
            return localContainer;
        }
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            await Task.Delay(1).ConfigureAwait(false);
            var response = await local.ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
            return response;
        }
    }
}
