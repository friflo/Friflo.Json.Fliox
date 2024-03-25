// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    internal static class ClusterUtils
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
                endpoint    = subscriber.Endpoint,
                messageSubs = msgSubs,
                changeSubs  = changeSubs
            };
        }
        
        // --- RequestCount
        internal static void UpdateCountsMap (
            Dictionary<ShortString, RequestCount>   requestCounts, // key: database
            in ShortString                          database,
            SyncRequest                             syncRequest)
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
            List<RequestCount>                      dst,
            Dictionary<ShortString, RequestCount>   src,
            string                                  exclude)
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
            dst.Sort((c1, c2) => c1.db.Compare(c2.db));
        }

        internal static List<ChangeType> FlagsToList(EntityChange flags) {
            var result = new List<ChangeType>(4);
            if ((flags & EntityChange.create) != 0) result.Add(ChangeType.create);
            if ((flags & EntityChange.upsert) != 0) result.Add(ChangeType.upsert);
            if ((flags & EntityChange.merge)  != 0) result.Add(ChangeType.merge);
            if ((flags & EntityChange.delete) != 0) result.Add(ChangeType.delete);
            return result;
        }
    }
}