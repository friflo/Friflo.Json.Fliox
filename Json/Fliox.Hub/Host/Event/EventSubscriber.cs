// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal enum TriggerType {
        None,
        Finish,
        Event
    }
    
    internal sealed class EventSubscriber {
        internal readonly   JsonKey                                 clientId;
        private             IEventTarget                            eventTarget;
        /// key: <see cref="SubscribeChanges.container"/>
        internal readonly   Dictionary<string, SubscribeChanges>    changeSubscriptions         = new Dictionary<string, SubscribeChanges>();
        internal readonly   HashSet<string>                         messageSubscriptions        = new HashSet<string>();
        internal readonly   HashSet<string>                         messagePrefixSubscriptions  = new HashSet<string>();
        private  readonly   Pool                                    pool;
        private  readonly   SharedCache                             sharedCache;
        private  readonly   HubLogger                               hubLogger;
        
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

        internal            int                                     Seq             => eventCounter;
        internal            int                                     EventQueueCount => eventQueue.Count;
        public   override   string                                  ToString()      => clientId.ToString();
        
        internal            int                                     SentEventsCount => sentEvents.Count;
        internal            bool                                    IsRemoteTarget  => eventTarget is WebSocketHost;
        
        internal List<ChangeSubscription> GetChangeSubscriptions (List<ChangeSubscription> subs) {
            if (changeSubscriptions.Count == 0)
                return null;
            if (subs == null) subs = new List<ChangeSubscription>(changeSubscriptions.Count);
            subs.Clear();
            subs.Capacity = changeSubscriptions.Count;
            foreach (var pair in changeSubscriptions) {
                SubscribeChanges sub = pair.Value;
                var changeSubscription = new ChangeSubscription {
                    container   = sub.container,
                    changes     = sub.changes,
                    filter      = sub.filterOp?.Linq
                };
                subs.Add(changeSubscription);
            }
            return subs;
        }

        internal EventSubscriber (SharedEnv env, in JsonKey clientId, IEventTarget eventTarget, bool background) {
            pool                = new Pool(env.Pool);
            sharedCache         = env.sharedCache;
            hubLogger           = env.hubLogger;
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
            if (this.eventTarget == null) throw new NullReferenceException(nameof(eventTarget));
            if (this.eventTarget == eventTarget)
                return;
            hubLogger.Log(HubLog.Info, $"EventSubscriber: eventTarget changed. dstId: {clientId}");
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
                    var executeContext  = new ExecuteContext(pool, eventTarget, sharedCache);
                    // In case the event target is remote connection it is not guaranteed that the event arrives.
                    // The remote target may already be disconnected and this is still not know when sending the event.
                    await eventTarget.ProcessEvent(ev, executeContext).ConfigureAwait(false);
                    
                    executeContext.Release();
                }
                catch (Exception e) {
                    var error = e.ToString();
                    hubLogger.Log(HubLog.Error, error);
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
                        hubLogger.Log(HubLog.Info, $"TriggerLoop() returns. {trigger}");
                        return;
                    }
                } catch (Exception e) {
                    hubLogger.Log(HubLog.Error, e.Message);
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