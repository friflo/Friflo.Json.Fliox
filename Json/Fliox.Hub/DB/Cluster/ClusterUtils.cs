// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host.Event;

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    public static class ClusterUtils
    {
        internal static SubscriptionEvents? GetSubscriptionEvents (EventSubClient subscriber, SubscriptionEvents? subscriptionEvents) {
            var changeSubs  = subscriptionEvents?.changeSubs;
            var msgSubs     = subscriptionEvents?.messageSubs;
            foreach (var pair in subscriber.databaseSubs) {
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
    }
}