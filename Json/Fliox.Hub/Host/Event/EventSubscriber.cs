// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Threading;

// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal enum TriggerType {
        None,
        Finish,
        Event
    }
    
    internal sealed class EventSubscriber : ILogSource {
        internal readonly   JsonKey                             clientId;
        private             IEventTarget                        eventTarget;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger                          Logger { get; }
        
        internal readonly   Dictionary<string, DatabaseSubs>    databaseSubs        = new Dictionary<string, DatabaseSubs>();
        
        internal            int                                 SubCount   => databaseSubs.Sum(sub => sub.Value.SubCount); 
        
        /// lock (<see cref="eventQueue"/>) {
        private             int                                 eventCounter;
        private  readonly   LinkedList<ProtocolEvent>           eventQueue = new LinkedList<ProtocolEvent>();
        /// contains all events which are sent but not acknowledged
        private  readonly   List<ProtocolEvent>                 sentEvents = new List<ProtocolEvent>();
        // }
        
        private  readonly   bool                                background;
        internal readonly   Task                                triggerLoop;
        private  readonly   DataChannelWriter<TriggerType>      triggerWriter;

        internal            int                                 Seq             => eventCounter;
        internal            int                                 EventQueueCount => eventQueue.Count;
        public   override   string                              ToString()      => clientId.ToString();
        
        internal            int                                 SentEventsCount => sentEvents.Count;
        internal            bool                                IsRemoteTarget  => eventTarget is WebSocketHost;
        
        internal EventSubscriber (SharedEnv env, in JsonKey clientId, IEventTarget eventTarget, bool background) {
            Logger              = env.hubLogger;
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
        
        internal void UpdateTarget(IEventTarget eventTarget) {
            if (this.eventTarget == null) throw new ArgumentNullException(nameof(eventTarget));
            if (this.eventTarget == eventTarget)
                return;
            Logger.Log(HubLog.Info, $"EventSubscriber: eventTarget changed. dstId: {clientId}");
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
                    // In case the event target is remote connection it is not guaranteed that the event arrives.
                    // The remote target may already be disconnected and this is still not know when sending the event.
                    await eventTarget.ProcessEvent(ev).ConfigureAwait(false);
                }
                catch (Exception e) {
                    var message = "SendEvents failed";
                    Logger.Log(HubLog.Error, message, e);
                    Debug.Fail($"{message}, exception: {e}");
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
                        Logger.Log(HubLog.Info, $"TriggerLoop() returns. {trigger}");
                        return;
                    }
                } catch (Exception e) {
                    var message = "TriggerLoop() failed";
                    Logger.Log(HubLog.Error, message, e);
                    Debug.Fail(message, e.Message);
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