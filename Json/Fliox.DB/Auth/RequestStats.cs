// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Auth
{
    public struct RequestStats {
        public            string              db;
        public            int                 requests;
        public            int                 tasks;
        
        internal static void Update (
            Dictionary<EntityDatabase, RequestStats>    stats,
            EntityDatabase                              db,
            SyncRequest                                 syncRequest)
        {
            if (!stats.TryGetValue(db, out RequestStats requestStats)) {
                requestStats = new RequestStats { db = db.name };
                stats.TryAdd(db, requestStats);
            }
            requestStats.requests  ++;
            requestStats.tasks     += syncRequest.tasks.Count;
            stats[db] = requestStats;
        }
        
        internal static void StatsToList(
            List<RequestStats>                          dst,
            Dictionary<EntityDatabase, RequestStats>    src,
            string                                      exclude)
        {
            dst.Clear();
            foreach (var pair in src) {
                var stats = pair.Value;
                if (stats.db == exclude)
                    continue;
                dst.Add(stats);
            }
        }
    }
}
