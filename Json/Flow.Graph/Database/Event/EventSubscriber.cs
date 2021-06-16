// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
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

        /// lock (<see cref="eventQueue"/>) {
        private             int                                     eventCounter;
        private  readonly   LinkedList<DatabaseEvent>               eventQueue = new LinkedList<DatabaseEvent>();
        private  readonly   List<DatabaseEvent>                     sentEvents = new List<DatabaseEvent>();
        // }

        public   override   string                                  ToString() => clientId;

        public EventSubscriber (string clientId, IEventTarget eventTarget) {
            this.clientId       = clientId;
            this.eventTarget    = eventTarget;
        }
        
        internal void UpdateTarget(IEventTarget eventTarget) {
            if (this.eventTarget == eventTarget)
                return;
            Console.WriteLine($"EventSubscriber: eventTarget changed. clientId: {clientId}");
            this.eventTarget = eventTarget;
        }
        
        internal void EnqueueEvent(DatabaseEvent ev) {
            lock (eventQueue) {
                ev.seq = ++eventCounter;
                eventQueue.AddLast(ev);
            }
        }
        
        private bool DequeueEvent(out DatabaseEvent ev) {
            lock (eventQueue) {
                var node = eventQueue.First;
                if (node == null) {
                    ev = null;
                    return false;
                }
                ev = node.Value;
                eventQueue.RemoveFirst();
                sentEvents.Add(ev);
                return true;
            }
        }

        internal void AcknowledgeEvent(int eventAck) {
            lock (eventQueue) {
                for (int i = sentEvents.Count - 1; i >= 0; i--) {
                    var ev = sentEvents[i];
                    if (ev.seq > eventAck) {
                        eventQueue.AddFirst(ev);
                    }
                }
                sentEvents.Clear();
            }
        }
        
        internal async Task SendEvents () {
            // early out in case the target is a remote connection which already closed.
            if (!eventTarget.IsOpen())
                return;
            
            var contextPools    = new Pools(Pools.SharedPools);
            while (DequeueEvent(out var ev)) {
                try {
                    var syncContext = new SyncContext(contextPools, eventTarget);
                    // In case the event target is remote connection it is not guaranteed that the event arrives.
                    // The remote target may already be disconnected and this is still not know when sending the event.
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