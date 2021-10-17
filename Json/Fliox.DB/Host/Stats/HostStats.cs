// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Host.Stats
{
    public class HostStats
    {
        public readonly RequestHistories    requestHistories = new RequestHistories();
        public          RequestCount        requestCount = new RequestCount();

        public void Update(SyncRequest syncRequest) {
            requestHistories.Update();
            requestCount.Update(syncRequest);
        }

        public void ClearHostStats() {
            requestHistories.ClearRequestHistories();
            requestCount = new RequestCount();
        }
    }
}