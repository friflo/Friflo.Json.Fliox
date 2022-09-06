// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.DB.Monitor
{
    // Is placed here in namespace DB.Monitor as it fits better than in Host.Stats
    /// <summary>number of requests and tasks executed per database</summary>
    public struct RequestCount {
        /// <summary>database name</summary>
        public              string  db;
        /// <summary>number of executed requests</summary>
        public              int     requests;
        /// <summary>number of executed tasks</summary>
        public              int     tasks;

        public override     string  ToString() => $"db: {db}, requests: {requests}, tasks: {tasks}";
    }
    
    internal static class RequestCountUtils {

        internal static void UpdateCountsMap (
            IDictionary<string, RequestCount>   requestCounts, // key: database
            string                              database,
            SyncRequest                         syncRequest)
        {
            if (!requestCounts.TryGetValue(database, out RequestCount requestCount)) {
                requestCount = new RequestCount { db = database };
                requestCounts.TryAdd(database, requestCount);
            }
            UpdateCounts(ref requestCount, syncRequest);
            requestCounts[database] = requestCount;
        }
        
        internal static void UpdateCounts(ref RequestCount counts, SyncRequest syncRequest) {
            counts.requests  ++;
            counts.tasks     += syncRequest.tasks.Count;
        }
        
        internal static void CountsMapToList(
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
