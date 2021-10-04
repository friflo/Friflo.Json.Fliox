// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host.Utils;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host.Event
{
    internal enum TriggerType {
        None,
        Finish,
        Event
    }
    
    public sealed class EventSubscriber {
        internal readonly   JsonKey                                 dstId;
        private             IEventTarget                            eventTarget;
        /// key: <see cref="SubscribeChanges.container"/>
        internal readonly   Dictionary<string, SubscribeChanges>    changeSubscriptions         = new Dictionary<string, SubscribeChanges>();
        internal readonly   HashSet<string>                         messageSubscriptions        = new HashSet<string>();
        internal readonly   HashSet<string>                         messagePrefixSubscriptions  = new HashSet<string>();
        private  readonly   Pools                                   pools                       = new Pools(Pools.SharedPools);
        
        internal            int                                     SubscriptionCount => changeSubscriptions.Count + messageSubscriptions.Count + messagePrefixSubscriptions.Count; 
        
        /// lock (<see cref="eventQueue"/>) {
        private             int                                     eventCounter;
        private  readonly   LinkedList<ProtocolEvent>               eventQueue = new LinkedList<ProtocolEvent>();
        /// contains all events which are sent but not acknowledged
        private  readonly   List<ProtocolEvent>                     sentEvents = new List<ProtocolEvent>();
        // }
        
        private  readonly   bool                                    background;
        internal readonly   Task                                    triggerLoop;
        private  readonly   DataChannelWriter<TriggerType>          triggerWriter;

        public   override   string                                  ToString() => dstId.ToString();
        
        /// used for test assertion
        public              int                                     SentEventsCount => sentEvents.Count;

        public EventSubscriber (in JsonKey dstId, IEventTarget eventTarget, bool background) {
            this.dstId          = dstId;
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
            Console.WriteLine($"EventSubscriber: eventTarget changed. dstId: {dstId}");
            this.eventTarget = eventTarget;
        }
        
        internal void EnqueueEvent(ProtocolEvent ev) {
            lock (eventQueue) {
                ev.seq = ++eventCounter;
                eventQueue.AddLast(ev);
                if (background) {
                    EnqueueTrigger(TriggerType.Event);
                }
            }
        }
        
        private bool DequeueEvent(out ProtocolEvent ev) {
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
                for (int i = sentEvents.Count - 1; i >= 0; i--) {
                    var ev = sentEvents[i];
                    if (ev.seq <= eventAck)
                        continue;
                    eventQueue.AddFirst(ev);
                }
                sentEvents.Clear();
                if (background && eventQueue.Count > 0) {
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