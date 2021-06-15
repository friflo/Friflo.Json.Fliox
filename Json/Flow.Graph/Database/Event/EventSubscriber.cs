// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Event
{
    public class EventSubscriber {
        internal readonly   string                                  clientId;
        private             IEventTarget                            eventTarget;
        /// key: <see cref="SubscribeChanges.container"/>
        internal readonly   Dictionary<string, SubscribeChanges>    subscriptions = new Dictionary<string, SubscribeChanges>();
        private  readonly   ConcurrentQueue<DatabaseEvent>          eventQueue = new ConcurrentQueue<DatabaseEvent>();
        private             int                                     eventCounter;

        public   override   string                                  ToString() => clientId;

        public EventSubscriber (string clientId, IEventTarget eventTarget) {
            this.clientId       = clientId;
            this.eventTarget    = eventTarget;
        }
        
        internal void EnqueueEvent(DatabaseEvent ev) {
            ev.eventId = ++eventCounter;
            eventQueue.Enqueue(ev);
        }
        
        internal void UpdateTarget(IEventTarget eventTarget) {
            if (this.eventTarget == eventTarget)
                return;
            Console.WriteLine($"client changed event target. clientId: {clientId}");
            this.eventTarget = eventTarget;
        }
        
        internal void Subscribe(SubscribeChanges subscribe) {
            subscriptions[subscribe.container] = subscribe;
        }
        
        internal async Task SendEvents () {
            if (!eventTarget.IsOpen())
                return;
            
            var contextPools    = new Pools(Pools.SharedPools);
            while (eventQueue.TryPeek(out var ev)) {
                try {
                    eventQueue.TryDequeue(out _);
                    var syncContext = new SyncContext(contextPools, eventTarget);
                    await eventTarget.ProcessEvent(ev, syncContext).ConfigureAwait(false);
                    
                    syncContext.pools.AssertNoLeaks();
                }
                catch (Exception e) {
                    var error = e.ToString();
                    Console.WriteLine(error);
                    Debug.Fail(error);
                }
            }
        }
    }
}