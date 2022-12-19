// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable InlineTemporaryVariable
// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace Friflo.Json.Fliox.Hub.Host.Event
{
    /// <summary>
    /// Each <see cref="EventSubClient"/> instance (Event Subscriber Client) - handle the subscriptions for a specific client. <br/>
    /// It send database changes and messages as events to the client for all subscriptions the client made. <br/>
    /// A client is identified by its <see cref="clientId"/>.
    /// </summary>
    internal sealed class EventSubClient : ILogSource {
        internal readonly   JsonKey                             clientId;   // key field
        internal readonly   EventSubUser                        user;
        internal            bool                                queueEvents;
        private             EventReceiver                       eventReceiver; // can be null if created by a REST request

        public              bool                                Connected => eventReceiver?.IsOpen() ?? false;
        [DebuggerBrowsable(Never)]
        public              IHubLogger                          Logger { get; }
        
        /// key: database - concurrent: database subs may change while running <see cref="EventDispatcher.EnqueueSyncTasks"/>
        [DebuggerBrowsable(Never)]
        internal readonly   ConcurrentDictionary<SmallString, DatabaseSubs> databaseSubs;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<DatabaseSubs>           DatabaseSubs => databaseSubs.Values;
        
        internal            int                                 SubCount    => databaseSubs.Sum(sub => sub.Value.SubCount); 
        
        /// lock (<see cref="unsentEventsDeque"/>) {
        private             int                                 eventCounter;
        /// <summary>contains all serialized <see cref="SyncEvent"/>'s not yet sent.
        /// TMeta is <see cref="SyncEvent.seq"/> </summary> 
        private  readonly   MessageBufferQueue<int>             unsentEventsDeque   = new MessageBufferQueue<int>();
        /// <summary>contains all serialized <see cref="SyncEvent"/>'s which are sent but not acknowledged.
        /// TMeta is <see cref="SyncEvent.seq"/></summary>
        private  readonly   MessageBufferQueue<int>             sentEventsQueue     = new MessageBufferQueue<int>();
        // }
        
        private  readonly   EventDispatcher                     dispatcher;

        internal            int                                 Seq                 => eventCounter;
        /// <summary> number of events stored for a client not yet acknowledged by the client </summary>
        internal            int                                 QueuedEventsCount   => unsentEventsDeque.Count + sentEventsQueue.Count;
        /// <summary>
        /// <b>true</b>  if eventReceiver is null or a remote target (WebSocket). <br/>
        /// <b>false</b> if the eventReceiver is provided by a FlioxClient (in process) 
        /// </summary>
        internal            bool                                SerializeEvents     => eventReceiver?.IsRemoteTarget() ?? true;
        
        public   override   string                              ToString()          => $"client: '{clientId.AsString()}'";
        
        
        internal EventSubClient (
            SharedEnv           env,
            EventSubUser        user,
            in JsonKey          clientId,
            EventDispatcher     dispatcher)
        {
            Logger              = env.hubLogger;
            this.clientId       = clientId;
            this.user           = user;
            this.dispatcher     = dispatcher;
            databaseSubs        = new ConcurrentDictionary<SmallString, DatabaseSubs>(SmallString.Equality);
        }
        
        internal bool UpdateTarget(EventReceiver eventReceiver) {
            if (eventReceiver == null) throw new ArgumentNullException(nameof(eventReceiver));
            if (this.eventReceiver == eventReceiver)
                return false;
            var msg = this.eventReceiver != null ?
                $"eventReceiver changed - client id: {clientId}" :
                $"eventReceiver new     - client id: {clientId}";
            this.eventReceiver = eventReceiver;
            Logger.Log(HubLog.Info, msg);
            SendUnacknowledgedEvents();
            return true;
        }
        
        internal void EnqueueEvent(ref SyncEvent ev, bool serializedEvents, ObjectWriter writer) {
            lock (unsentEventsDeque) {
                ev.seq = ++eventCounter;
                var rawEvent = RemoteUtils.SerializeSyncEvent(ev, serializedEvents, writer);
                unsentEventsDeque.AddTail(rawEvent, ev.seq);
            }
            // Signal new event. Need to be signaled after adding event to queue. No reason to execute this in the lock. 
            if (dispatcher != null) {
                dispatcher.NewClientEvent(this);
            }
        }
        
        private bool DequeueEvents(List<JsonValue> events) {
            var deque = unsentEventsDeque;
            events.Clear();
            lock (deque) {
                var count = deque.Count;
                if (count == 0) {
                    return false;
                }
                if (count > 100) count = 100;
                for (int n = 0; n < count; n++) {
                    var ev = deque.RemoveHead();
                    events.Add(ev.value);
                    if (queueEvents) {
                        sentEventsQueue.AddTail(ev.value, ev.meta);
                    }
                } 
                return true;
            }
        }
        
        /// <summary>
        /// Remove all acknowledged events from <see cref="sentEventsQueue"/>
        /// </summary>
        internal void AcknowledgeEvents(int eventAck) {
            lock (unsentEventsDeque) {
                while (sentEventsQueue.Count > 0) {
                    var ev = sentEventsQueue.GetHead();
                    if (ev.meta <= eventAck) {
                        sentEventsQueue.RemoveHead();
                        continue;
                    }
                    break; 
                }
            }
        }

        /// <summary>
        /// Prepend all not acknowledged events to <see cref="unsentEventsDeque"/> in their original order
        /// and trigger sending the events stored in the deque.
        /// </summary>
        private void SendUnacknowledgedEvents() {
            var deque = unsentEventsDeque;
            lock (deque) {
                deque.AddHeadQueue(sentEventsQueue);
                sentEventsQueue.Clear();
                if (dispatcher != null && deque.Count > 0) {
                    // Console.WriteLine($"unsentEventsQueue: {unsentEventsQueue.Count}");
                    // Trace.WriteLine($"*** SendUnacknowledgedEvents. Count: {unsentEventsQueue.Count}");
                    dispatcher.NewClientEvent(this);
                }
            }
        }
        
        internal void SendEvents (ObjectMapper mapper, List<JsonValue> events) {
            var receiver = eventReceiver;
            // early out in case the target is a remote connection which already closed.
            if (receiver == null || !receiver.IsOpen()) {
                if (queueEvents)
                    return;
                lock (unsentEventsDeque) {
                    unsentEventsDeque.Clear();
                }
                return;
            }
            // Trace.WriteLine("--- SendEvents");
            while (DequeueEvents(events)) {
                // var msg = $"DequeueEvent {ev.seq}";
                // Trace.WriteLine(msg);
                // Console.WriteLine(msg);
                try {
                    // Console.WriteLine($"--- SendEvents: {events.Length}");
                    // In case the event target is remote connection it is not guaranteed that the event arrives.
                    // The remote target may already be disconnected and this is still not know when sending the event.
                    var rawEventMessage = RemoteUtils.CreateProtocolEvent(events, clientId, mapper);
                    var clientEvent     = new RemoteEvent(clientId, rawEventMessage);
                    receiver.SendEvent(clientEvent);
                }
                catch (Exception e) {
                    var message = "SendEvents failed";
                    Logger.Log(HubLog.Error, message, e);
                    Debug.Fail($"{message}, exception: {e}");
                }
            }
        }
    }
}