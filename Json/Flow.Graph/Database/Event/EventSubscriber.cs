// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Event
{
    internal enum TriggerType {
        Finished,
        Event
    }
    
    public class EventSubscriber {
        internal readonly   string                                  clientId;
        private             IEventTarget                            eventTarget;
        /// key: <see cref="SubscribeChanges.container"/>
        internal readonly   Dictionary<string, SubscribeChanges>    subscriptions = new Dictionary<string, SubscribeChanges>();
        
        private  readonly   bool                                    background;

        /// lock (<see cref="eventQueue"/>) {
        private             int                                     eventCounter;
        private  readonly   LinkedList<DatabaseEvent>               eventQueue = new LinkedList<DatabaseEvent>();
        /// contains all events which are sent but not acknowledged
        private  readonly   List<DatabaseEvent>                     sentEvents = new List<DatabaseEvent>();
        // }
        
        private  readonly   Task                                    triggerQueue;
        private  readonly   ChannelWriter<TriggerType>              triggerWriter;

        public   override   string                                  ToString() => clientId;

        public EventSubscriber (string clientId, IEventTarget eventTarget, bool background) {
            this.clientId       = clientId;
            this.eventTarget    = eventTarget;
            this.background     = background;
            if (this.background) {
                // --- Task queue
                var opt = new UnboundedChannelOptions { SingleReader = true, SingleWriter = true };
                // opt.AllowSynchronousContinuations = true;
                var channel         = Channel.CreateUnbounded<TriggerType>(opt);
                triggerWriter       = channel.Writer;
                var triggerReader   = channel.Reader;
                triggerQueue        = RunQueue(triggerReader);
            }
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
                if (background) {
                    EnqueueTrigger(TriggerType.Event);
                }
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
        
        /// Enqueue all not acknowledged events back to <see cref="eventQueue"/> in their original order
        internal void AcknowledgeEvents(int eventAck) {
            lock (eventQueue) {
                bool pendingSendEvents = false;
                for (int i = sentEvents.Count - 1; i >= 0; i--) {
                    var ev = sentEvents[i];
                    if (ev.seq > eventAck) {
                        eventQueue.AddFirst(ev);
                        pendingSendEvents = true;
                    }
                }
                sentEvents.Clear();
                if (background && pendingSendEvents) {
                    EnqueueTrigger (TriggerType.Event);
                }
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
        
        // ------------------------------------- Task queue -------------------------------------
        private Task RunQueue(ChannelReader<TriggerType> triggerReader) {
            var runQueue = Task.Run(async () => {
                while (true) {
                    var entry = await triggerReader.ReadAsync();
                    if (entry == TriggerType.Finished)
                        return;
                    await SendEvents();
                }
            });
            return runQueue;
        }
        
        private void EnqueueTrigger(TriggerType entry) {
            bool success = triggerWriter.TryWrite(entry);
            if (success)
                return;
            Debug.Fail("EnqueueTrigger() - writer.TryWrite() failed");
        }
    }
}