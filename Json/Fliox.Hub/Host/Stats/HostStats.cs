// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Host.Stats
{
    internal sealed class HostStats
    {
        internal readonly   RequestHistories    requestHistories = new RequestHistories();
        internal            RequestCount        requestCount = new RequestCount();

        internal void Update(SyncRequest syncRequest) {
            requestHistories.Update();
            requestCount.Update(syncRequest);
        }

        internal void ClearHostStats() {
            requestHistories.ClearRequestHistories();
            requestCount = new RequestCount();
        }
    }
}