// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    /// <summary>
    /// Set of utility methods to guide a graceful shutdown by disposing all resources.
    /// The intended order for shutdown is:
    /// <list type="bullet">
    ///     <item><see cref="DisposeStore"/></item>
    ///     <item><see cref="DisposeDatabase"/></item>
    ///     <item><see cref="DisposeCaches"/></item>
    /// </list>  
    /// </summary>
    public static class DisposeUtils
    {
        public static async Task DisposeStore(EntityStore store) {
            if (store == null)
                return;
            await store.CancelPendingSyncs();
            store.Dispose();
        }
        
        public static void DisposeDatabase(EntityDatabase database) {
            if (database == null)
                return;
            if (database.eventBroker != null) {
                var eb = database.eventBroker;
                database.eventBroker = null;
                eb.Dispose();
            }
            database.Dispose();
        }
        
        public static void DisposeCaches() {
            Pools.SharedPools.Dispose();
            SyncTypeStore.Dispose();
        }
    }
}