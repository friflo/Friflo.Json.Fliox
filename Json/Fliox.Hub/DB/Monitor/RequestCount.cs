// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.DB.Monitor
{
    // Is placed here in namespace DB.Monitor as it fits better than in Host.Stats
    public struct RequestCount {
        public              string  db;
        public              int     requests;
        public              int     tasks;

        public override     string  ToString() => $"db: {db}, requests: {requests}, tasks: {tasks}";

        internal static void UpdateCounts (
            IDictionary<string, RequestCount>   requestCounts, // key: database
            string                              database,
            SyncRequest                         syncRequest)
        {
            if (!requestCounts.TryGetValue(database, out RequestCount requestCount)) {
                requestCount = new RequestCount { db = database };
                requestCounts.TryAdd(database, requestCount);
            }
            requestCount.Update(syncRequest);
            requestCounts[database] = requestCount;
        }
        
        internal void Update(SyncRequest syncRequest) {
            requests  ++;
            tasks     += syncRequest.tasks.Count;
        }
        
        internal static void CountsToList(
            List<RequestCount>                  dst,
            IDictionary<string, RequestCount>   src,
            string                              exclude)
        {
            dst.Clear();
            foreach (var pair in src) {
                var counts = pair.Value;
                /* if (exclude != null && counts.db == exclude)
                    continue; */
                dst.Add(counts);
            }
            dst.Sort((c1, c2) => string.Compare(c1.db, c2.db, StringComparison.Ordinal));
        }
    }
}
