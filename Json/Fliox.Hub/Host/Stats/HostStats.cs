// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Host.Stats
{
    internal sealed class HostStats
    {
        internal  readonly  RequestHistories    requestHistories = new RequestHistories();
        internal            RequestCount        requestCount;
        
        public    override  string              ToString() => requestCount.ToString();

        internal void Update(SyncRequest syncRequest) {
            requestHistories.Update();
            ClusterUtils.UpdateCounts(ref requestCount, syncRequest);
        }

        internal void ClearHostStats() {
            requestHistories.ClearRequestHistories();
            requestCount = new RequestCount();
        }
    }
}