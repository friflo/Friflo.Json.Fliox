// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Protocol;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.DB.Host.Stats
{
    public struct RequestCount {
        public              string  db;
        public              int     requests;
        public              int     tasks;

        public override     string  ToString() => $"db: {db}, requests: {requests}, tasks: {tasks}";

        internal static void Update (
            Dictionary<string, RequestCount>    requestCounts,
            string                              database,
            SyncRequest                         syncRequest)
        {
            if (!requestCounts.TryGetValue(database, out RequestCount requestStats)) {
                requestStats = new RequestCount { db = database };
                requestCounts.TryAdd(database, requestStats);
            }
            requestStats.requests  ++;
            requestStats.tasks     += syncRequest.tasks.Count;
            requestCounts[database] = requestStats;
        }
        
        internal static void CountsToList(
            List<RequestCount>                  dst,
            Dictionary<string, RequestCount>    src,
            string                              exclude)
        {
            dst.Clear();
            foreach (var pair in src) {
                var counts = pair.Value;
                /* if (exclude != null && counts.db == exclude)
                    continue; */
                dst.Add(counts);
            }
        }
    }
}
