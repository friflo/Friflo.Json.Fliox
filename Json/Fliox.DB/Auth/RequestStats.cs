// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Protocol;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.DB.Auth
{
    public struct RequestStats {
        public              string  db;
        public              int     requests;
        public              int     tasks;

        public override     string  ToString() => $"db: {db}, requests: {requests}, tasks: {tasks}";

        internal static void Update (
            Dictionary<string, RequestStats>    stats,
            string                              database,
            SyncRequest                         syncRequest)
        {
            if (!stats.TryGetValue(database, out RequestStats requestStats)) {
                requestStats = new RequestStats { db = database };
                stats.TryAdd(database, requestStats);
            }
            requestStats.requests  ++;
            requestStats.tasks     += syncRequest.tasks.Count;
            stats[database] = requestStats;
        }
        
        internal static void StatsToList(
            List<RequestStats>                  dst,
            Dictionary<string, RequestStats>    src,
            string                              exclude)
        {
            dst.Clear();
            foreach (var pair in src) {
                var stats = pair.Value;
                /* if (exclude != null && stats.db == exclude)
                    continue; */
                dst.Add(stats);
            }
        }
    }
}
