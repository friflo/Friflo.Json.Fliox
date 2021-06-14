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
        private  readonly   IEventTarget                            eventTarget;
        /// key: <see cref="SubscribeChanges.container"/>
        internal readonly   Dictionary<string, SubscribeChanges>    subscribeMap = new Dictionary<string, SubscribeChanges>();
        internal readonly   ConcurrentQueue<DatabaseEvent>          eventQueue = new ConcurrentQueue<DatabaseEvent>();
        
        public EventSubscriber (string clientId, IEventTarget eventTarget) {
            this.clientId       = clientId;
            this.eventTarget    = eventTarget;
        }
        
        internal async Task SendEvents () {
            if (!eventTarget.IsOpen())
                return;
            
            var contextPools    = new Pools(Pools.SharedPools);
            while (eventQueue.TryPeek(out var ev)) {
                try {
                    var syncContext     = new SyncContext(contextPools, eventTarget);
                    var success = await eventTarget.ProcessEvent(ev, syncContext).ConfigureAwait(false);
                    if (success) {
                        eventQueue.TryDequeue(out _);
                    }
                    syncContext.pools.AssertNoLeaks();
                } catch (Exception e) {
                    var error = e.ToString();
                    Console.WriteLine(error);
                    Debug.Fail(error);
                }
            }
        }
    }
}