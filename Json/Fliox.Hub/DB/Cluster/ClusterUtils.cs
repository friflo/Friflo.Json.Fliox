// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    public static class ClusterUtils
    {
        // --- SubscriptionEvents
        internal static SubscriptionEvents? GetSubscriptionEvents (
            EventDispatcher     dispatcher,
            EventSubClient      subscriber,
            SubscriptionEvents? subscriptionEvents)
        {
            var changeSubs  = subscriptionEvents?.changeSubs;
            var msgSubs     = subscriptionEvents?.messageSubs;
            var subsMap     = dispatcher.GetDatabaseSubs(subscriber);
            foreach (var pair in subsMap) {
                var databaseSubs = pair.Value;
                msgSubs     = databaseSubs.GetMessageSubscriptions(msgSubs);
                changeSubs  = databaseSubs.GetChangeSubscriptions (changeSubs);
            }
            return new SubscriptionEvents {
                seq         = subscriber.Seq,
                queued      = subscriber.QueuedEventsCount,
                queueEvents = subscriber.queueEvents,
                connected   = subscriber.Connected,
                messageSubs = msgSubs,
                changeSubs  = changeSubs
            };
        }
        
        // --- RequestCount
        internal static void UpdateCountsMap (
            Dictionary<JsonKey, RequestCount>   requestCounts, // key: database
            in JsonKey                          database,
            SyncRequest                         syncRequest)
        {
            lock (requestCounts) {
                if (!requestCounts.TryGetValue(database, out RequestCount requestCount)) {
                    requestCount = new RequestCount { db = database };
                    requestCounts[database] = requestCount;
                }
                UpdateCounts(ref requestCount, syncRequest);
                requestCounts[database] = requestCount;
            }
        }
        
        internal static void UpdateCounts(ref RequestCount counts, SyncRequest syncRequest) {
            counts.requests  ++;
            counts.tasks     += syncRequest.tasks.Count;
        }
        
        internal static void CountsMapToList(
            List<RequestCount>                  dst,
            Dictionary<JsonKey, RequestCount>   src,
            string                              exclude)
        {
            dst.Clear();
            lock (src) {
                foreach (var pair in src) {
                    var counts = pair.Value;
                    /* if (exclude != null && counts.db == exclude)
                        continue; */
                    dst.Add(counts);
                }
            }
            dst.Sort((c1, c2) => JsonKey.StringCompare(c1.db, c2.db, StringComparison.Ordinal));
        }
    }
}