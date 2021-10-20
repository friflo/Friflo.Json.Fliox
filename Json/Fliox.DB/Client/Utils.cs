// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Internal;

namespace Friflo.Json.Fliox.DB.Client
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
        public static async Task DisposeStore(FlioxClient store) {
            if (store == null)
                return;
            await store.CancelPendingSyncs().ConfigureAwait(false);
            store.Dispose();
        }
        
        public static void DisposeDatabase(DatabaseHub database) {
            if (database == null)
                return;
            if (database.EventBroker != null) {
                var eb = database.EventBroker;
                database.EventBroker = null;
                eb.Dispose();
            }
            database.Dispose();
        }
        
        public static void DisposeCaches() {
            UtilsInternal.SharedPools.Dispose();
            HostTypeStore.Dispose();
        }
    }
}