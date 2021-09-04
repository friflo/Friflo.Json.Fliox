// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Database.Utils;
using Friflo.Json.Fliox.DB.Sync;

namespace Friflo.Json.Fliox.DB.Database.Event
{
    internal enum TriggerType {
        None,
        Finish,
        Event
    }
    
    public class EventSubscriber {
        internal readonly   string                                  clientId;
        private             IEventTarget                            eventTarget;
        /// key: <see cref="SubscribeChanges.container"/>
        internal readonly   Dictionary<string, SubscribeChanges>    changeSubscriptions         = new Dictionary<string, SubscribeChanges>();
        internal readonly   HashSet<string>                         messageSubscriptions        = new HashSet<string>();
        internal readonly   HashSet<string>                         messagePrefixSubscriptions  = new HashSet<string>();
        private  readonly   Pools                                   pools                       = new Pools(Pools.SharedPools);
        
        internal            int                                     SubscriptionCount => changeSubscriptions.Count + messageSubscriptions.Count + messagePrefixSubscriptions.Count; 
        
        /// lock (<see cref="eventQueue"/>) {
        private             int                                     eventCounter;
        private  readonly   LinkedList<DatabaseEvent>               eventQueue = new LinkedList<DatabaseEvent>();
        /// contains all events which are sent but not acknowledged
        private  readonly   List<DatabaseEvent>                     sentEvents = new List<DatabaseEvent>();
        // }
        
        private  readonly   bool                                    background;
        internal readonly   Task                                    triggerLoop;
        private  readonly   DataChannelWriter<TriggerType>          triggerWriter;

        public   override   string                                  ToString() => clientId;
        
        /// used for test assertion
        public              int                                     SentEventsCount => sentEvents.Count;

        public EventSubscriber (string clientId, IEventTarget eventTarget, bool background) {
            this.clientId       = clientId;
            this.eventTarget    = eventTarget;
            this.background     = background;
            if (!this.background)
                return;
            // --- use trigger channel and loop
            var channel         = DataChannel<TriggerType>.CreateUnbounded(true, true);
            triggerWriter       = channel.writer;
            var triggerReader   = channel.reader;
            triggerLoop         = TriggerLoop(triggerReader);
        }
        
        internal bool FilterMessage (string messageName) {
            if (messageSubscriptions.Contains(messageName))
                return true;
            foreach (var prefixSub in messagePrefixSubscriptions) {
                if (messageName.StartsWith(prefixSub)) {
                    return true;
                }
            }
            return false;
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
                    if (ev.seq <= eventAck)
                        continue;
                    eventQueue.AddFirst(ev);
                    pendingSendEvents = true;
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
            
            while (DequeueEvent(out var ev)) {
                try {
                    var messageContext  = new MessageContext(pools, eventTarget);
                    // In case the event target is remote connection it is not guaranteed that the event arrives.
                    // The remote target may already be disconnected and this is still not know when sending the event.
                    await eventTarget.ProcessEvent(ev, messageContext).ConfigureAwait(false);
                    
                    messageContext.Release();
                }
                catch (Exception e) {
                    var error = e.ToString();
                    Console.WriteLine(error);
                    Debug.Fail(error);
                }
            }
        }
        
        // ---------------------------- trigger channel and queue ----------------------------
        private Task TriggerLoop(DataChannelReader<TriggerType> triggerReader) {
            var loopTask = Task.Run(async () => {
                try {
                    while (true) {
                        var trigger = await triggerReader.ReadAsync().ConfigureAwait(false);
                        if (trigger == TriggerType.Event) {
                            await SendEvents().ConfigureAwait(false);
                            continue;
                        }
                        Console.WriteLine($"TriggerLoop() returns. {trigger}");
                        return;
                    }
                } catch (Exception e) {
                    Debug.Fail("TriggerLoop() failed", e.Message);
                }
            });
            return loopTask;
        }
        
        private void EnqueueTrigger(TriggerType trigger) {
            bool success = triggerWriter.TryWrite(trigger);
            if (success)
                return;
            Debug.Fail("EnqueueTrigger() - writer.TryWrite() failed");
        }
        
        internal void FinishQueue() {
            EnqueueTrigger(TriggerType.Finish);
            triggerWriter.Complete();
        }
    }
}