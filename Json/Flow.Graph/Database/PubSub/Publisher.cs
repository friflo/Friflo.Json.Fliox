// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.PubSub
{
    public interface ISyncObserver
    {
        void EnqueueSyncRequest (SyncRequest syncRequest);
    }
    
    public class Subscriber {
        internal EntityDatabase                 database;
        internal Subscription                   subscription;
        internal ConcurrentQueue<SyncRequest>   queue = new ConcurrentQueue<SyncRequest>();
        
        internal async Task Publish () {
            while (queue.TryDequeue(out var syncRequest)) {
                var contextPools    = new Pools(Pools.SharedPools);
                var syncContext     = new SyncContext(contextPools);
                try {
                    await database.ExecuteSync(syncRequest, syncContext);
                } catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
    
    public class Publisher : ISyncObserver
    {
        public void Subscribe (EntityDatabase database, Subscription subscription) {
            var subscriber = new Subscriber {
                database        = database,
                subscription    = subscription
            };
            subscribers.Add(database, subscriber);
        }
        
        private readonly Dictionary<EntityDatabase, Subscriber> subscribers = new Dictionary<EntityDatabase, Subscriber>();
        
        public void EnqueueSyncRequest (SyncRequest syncRequest) {
            foreach (var pair in subscribers) {
                var subscriber = pair.Value;
                subscriber.queue.Enqueue(syncRequest);
            }
        }
    }
}