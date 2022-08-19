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
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal enum TriggerType {
        None,
        Finish,
        Event
    }

    /// <summary>
    /// Each <see cref="EventSubClient"/> instance (Event Subscriber Client) - handle the subscriptions for a specific client. <br/>
    /// It send database changes and messages as events to the client for all subscriptions the client made. <br/>
    /// A client is identified by its <see cref="clientId"/>.
    /// </summary>
    internal sealed class EventSubClient : ILogSource {
        internal readonly   JsonKey                             clientId;   // key field
        internal readonly   EventSubUser                        user;
        private             IEventReceiver                      eventReceiver;

        [DebuggerBrowsable(Never)]
        public              IHubLogger                          Logger { get; }
        
        /// key: database
        [DebuggerBrowsable(Never)]
        internal readonly   Dictionary<string, DatabaseSubs>    databaseSubs = new Dictionary<string, DatabaseSubs>();
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             IReadOnlyCollection<DatabaseSubs>   DatabaseSubs => databaseSubs.Values;
        
        internal            int                                 SubCount    => databaseSubs.Sum(sub => sub.Value.SubCount); 
        
        /// lock (<see cref="unsentEventsQueue"/>) {
        private             int                                 eventCounter;
        /// contains all events not yet sent. Should be a Dqueue but C# doesn't contain this container type. 
        private  readonly   LinkedList<ProtocolEvent>           unsentEventsQueue   = new LinkedList<ProtocolEvent>();
        /// contains all events which are sent but not acknowledged
        private  readonly   Queue<ProtocolEvent>                sentEventsQueue     = new Queue<ProtocolEvent>();
        // }
        
        private  readonly   bool                                background;
        internal readonly   Task                                triggerLoop;
        private  readonly   DataChannelWriter<TriggerType>      triggerWriter;

        internal            int                                 Seq                 => eventCounter;
        /// <summary> number of events stored for a client not yet acknowledged by the client </summary>
        internal            int                                 QueuedEventsCount   => unsentEventsQueue.Count + sentEventsQueue.Count;
        internal            bool                                IsRemoteTarget      => eventReceiver.IsRemoteTarget();
        
        public   override   string                              ToString()          => $"client: '{clientId.AsString()}'";
        
        
        internal EventSubClient (
            SharedEnv       env,
            EventSubUser    user,
            in JsonKey      clientId,
            IEventReceiver  eventReceiver,
            bool            background)
        {
            Logger              = env.hubLogger;
            this.clientId       = clientId;
            this.user           = user;
            this.eventReceiver  = eventReceiver;
            this.background     = background;
            if (!this.background)
                return;
            // --- use trigger channel and loop
            var channel         = DataChannel<TriggerType>.CreateUnbounded(true, true);
            triggerWriter       = channel.writer;
            var triggerReader   = channel.reader;
            triggerLoop         = TriggerLoop(triggerReader);
        }
        
        internal void UpdateTarget(IEventReceiver eventReceiver) {
            if (this.eventReceiver == null) throw new ArgumentNullException(nameof(eventReceiver));
            if (this.eventReceiver == eventReceiver)
                return;
            Logger.Log(HubLog.Info, $"EventSubscriber: eventReceiver changed. dstId: {clientId}");
            this.eventReceiver = eventReceiver;
            SendUnacknowledgedEvents();
        }
        
        internal void EnqueueEvent(ProtocolEvent ev) {
            lock (unsentEventsQueue) {
                ev.seq = ++eventCounter;
                unsentEventsQueue.AddLast(ev);
                if (background) {
                    EnqueueTrigger(TriggerType.Event);
                }
            }
        }
        
        private bool DequeueEvent(out ProtocolEvent ev) {
            lock (unsentEventsQueue) {
                var node = unsentEventsQueue.First;
                if (node == null) {
                    ev = null;
                    return false;
                }
                ev = node.Value;
                unsentEventsQueue.RemoveFirst();
                sentEventsQueue.Enqueue(ev);
                return true;
            }
        }
        
        /// <summary>
        /// Remove all acknowledged events from <see cref="sentEventsQueue"/>
        /// </summary>
        internal void AcknowledgeEvents(int eventAck) {
            lock (unsentEventsQueue) {
                while (sentEventsQueue.Count > 0) {
                    var ev = sentEventsQueue.Peek();
                    if (ev.seq <= eventAck) {
                        sentEventsQueue.Dequeue();
                        continue;
                    }
                    break; 
                }
            }
        }

        /// <summary>
        /// Prepend all not acknowledged events to <see cref="unsentEventsQueue"/> in their original order
        /// and trigger sending these events stored in <see cref="unsentEventsQueue"/>
        /// </summary>
        private void SendUnacknowledgedEvents() {
            lock (unsentEventsQueue) {
                LinkedListNode<ProtocolEvent> head = null;
                foreach (var ev in sentEventsQueue) {
                    if (head == null) {
                        head = unsentEventsQueue.AddFirst(ev);
                    } else  {
                        head = unsentEventsQueue.AddAfter(head, ev);
                    }
                }
                sentEventsQueue.Clear();
                if (background && unsentEventsQueue.Count > 0) {
                    // Console.WriteLine($"unsentEventsQueue: {unsentEventsQueue.Count}");
                    // Trace.WriteLine($"*** SendUnacknowledgedEvents. Count: {unsentEventsQueue.Count}");
                    EnqueueTrigger (TriggerType.Event);
                }
            }
        }
        
        internal async Task SendEvents () {
            // early out in case the target is a remote connection which already closed.
            if (!eventReceiver.IsOpen())
                return;
            // Trace.WriteLine("--- SendEvents");
            while (DequeueEvent(out var ev)) {
                // var msg = $"DequeueEvent {ev.seq}";
                // Trace.WriteLine(msg);
                // Console.WriteLine(msg);
                try {
                    // In case the event target is remote connection it is not guaranteed that the event arrives.
                    // The remote target may already be disconnected and this is still not know when sending the event.
                    await eventReceiver.ProcessEvent(ev).ConfigureAwait(false);
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