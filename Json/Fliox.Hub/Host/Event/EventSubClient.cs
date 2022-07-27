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
        /// contains all events not yet sent
        private  readonly   LinkedList<ProtocolEvent>           unsentEventsQueue   = new LinkedList<ProtocolEvent>();
        /// contains all events which are sent but not acknowledged
        private  readonly   List<ProtocolEvent>                 sentEventsList      = new List<ProtocolEvent>();
        // }
        
        private  readonly   bool                                background;
        internal readonly   Task                                triggerLoop;
        private  readonly   DataChannelWriter<TriggerType>      triggerWriter;

        internal            int                                 Seq                 => eventCounter;
        internal            int                                 QueuedEventsCount   => unsentEventsQueue.Count + sentEventsList.Count;
        
        public   override   string                              ToString()          => $"client: '{clientId.AsString()}'";
        
        internal            bool                                IsRemoteTarget      => eventReceiver is WebSocketHost;
        
        internal EventSubClient (
            SharedEnv       env,
            EventSubUser    user,
            in JsonKey      clientId,
            int             eventAck,
            IEventReceiver  eventReceiver,
            bool            background)
        {
            Logger              = env.hubLogger;
            this.clientId       = clientId;
            this.user           = user;
            this.eventCounter   = eventAck;
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
                sentEventsList.Add(ev);
                return true;
            }
        }
        
        /// Enqueue all not acknowledged events back to <see cref="unsentEventsQueue"/> in their original order
        internal void AcknowledgeEvents(int eventAck) {
            lock (unsentEventsQueue) {
                for (int i = sentEventsList.Count - 1; i >= 0; i--) {
                    var ev = sentEventsList[i];
                    if (ev.seq <= eventAck)
                        continue;
                    unsentEventsQueue.AddFirst(ev);
                }
                sentEventsList.Clear();
                if (background && unsentEventsQueue.Count > 0) {
                    EnqueueTrigger (TriggerType.Event);
                }
            }
        }
        
        internal async Task SendEvents () {
            // early out in case the target is a remote connection which already closed.
            if (!eventReceiver.IsOpen())
                return;
            
            while (DequeueEvent(out var ev)) {
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