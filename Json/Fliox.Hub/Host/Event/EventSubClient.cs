// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Friflo.Json.Fliox.Hub.Protocol;
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
    internal sealed class EventSubClient {
        internal readonly   ShortString                             clientId;   // key field
        internal readonly   EventSubUser                            user;
        internal            bool                                    queueEvents;
        private             IEventReceiver                          eventReceiver; // can be null if created by a REST request
        
        internal            string                                  Endpoint  => eventReceiver?.Endpoint;

        public              bool                                    Connected => eventReceiver?.IsOpen() ?? false;
        [DebuggerBrowsable(Never)]
        private  readonly   IHubLogger                              logger;
        /// <summary>
        /// key: database. <b>Note</b> requires lock <see cref="EventDispatcherIntern.monitor"/>. <br/>
        /// Use thread safe <see cref="EventDispatcher.GetDatabaseSubs"/>
        /// </summary> 
        [DebuggerBrowsable(Never)]
        internal readonly   Dictionary<ShortString, DatabaseSubs>   databaseSubs;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<DatabaseSubs>               DatabaseSubs => databaseSubs.Values;
        
        internal            int                                     SubCount    => databaseSubs.Sum(sub => sub.Value.SubCount); 
        
        /// lock (<see cref="unsentSyncEvents"/>) {
        private             int                                     eventCounter;
        /// <summary>Contains all serialized <see cref="SyncEvent"/>'s not yet sent.</summary>
        private  readonly   MessageBufferQueue<VoidMeta>            unsentSyncEvents    = new MessageBufferQueue<VoidMeta>();
        /// <summary>Contains all serialized <see cref="EventMessage"/>'s which are sent but not acknowledged.
        /// TMeta is <see cref="EventMessage.seq"/></summary>
        private  readonly   MessageBufferQueue<int>                 sentEventMessages   = new MessageBufferQueue<int>();
        /// <summary>Set to true if <see cref="sentEventMessages"/> need to be resend</summary>
        private             bool                                    resendEventMessages;
        // }
        
        private  readonly   EventDispatcher                         dispatcher;

        internal            int                                     Seq                 => eventCounter;
        /// <summary> number of events stored for a client not yet acknowledged by the client </summary>
        internal            int                                     QueuedEventsCount   => unsentSyncEvents.Count + sentEventMessages.Count;
        
        public   override   string                                  ToString()          => $"client: '{clientId.AsString()}'";
        
        
        internal EventSubClient (
            SharedEnv           env,
            EventSubUser        user,
            in ShortString      clientId,
            EventDispatcher     dispatcher)
        {
            logger              = env.hubLogger;
            this.clientId       = clientId;
            this.user           = user;
            this.dispatcher     = dispatcher;
            databaseSubs        = new Dictionary<ShortString, DatabaseSubs>(ShortString.Equality);
        }
        
        internal bool UpdateTarget(IEventReceiver eventReceiver) {
            if (eventReceiver == null) throw new ArgumentNullException(nameof(eventReceiver));
            var old = this.eventReceiver;
            if (old == eventReceiver)
                return false;
            var msg = old != null ?
                $"event receiver: changed  client: {clientId}  endpoint: {eventReceiver.Endpoint}  was: {old.Endpoint}" :
                $"event receiver: new      client: {clientId}  endpoint: {eventReceiver.Endpoint}";
            this.eventReceiver = eventReceiver;
            logger.Log(HubLog.Info, msg);
            SendUnacknowledgedEvents();
            return true;
        }
        
        /// <summary>Enqueue serialized <see cref="SyncEvent"/> for sending</summary>
        /// <remarks>
        /// Note: dispatcher is null in case using <see cref="EventDispatching.Queue"/>.<br/>
        /// So SyncEvent's are collected until calling <see cref="EventDispatcher.SendQueuedEvents"/>
        /// </remarks>
        internal void EnqueueSyncEvent(in JsonValue syncEvent) {
            lock (unsentSyncEvents) {
                unsentSyncEvents.AddTail(syncEvent);
            }
            // dispatcher == null  =>  see remarks
            dispatcher?.NewClientEvent(this);
        }
        
        /// <summary> Dequeue all queued messages. </summary>
        private bool DequeueEvents(in SendEventsContext context)
        {
            var syncEvents      = context.syncEvents;
            var eventMessages   = context.eventMessages;
            syncEvents.Clear();
            eventMessages.Clear();
            lock (unsentSyncEvents) {
                // resend EventMessage's in case of a reconnect and sent EventMessage's are queued.
                // reconnects should typically occur rarely   
                if (resendEventMessages) {
                    resendEventMessages = false;
                    // must be copied -> the byte[]'s in sentEventMessages may change outside lock
                    CopyEventMessages(eventMessages, sentEventMessages);
                }
                var syncEventCount = unsentSyncEvents.Count;
                if (syncEventCount > 0) {
                    unsentSyncEvents.DequeMessageValues(syncEvents);
                }
            }
            if (syncEvents.Count > 0) {
                int seq = ++eventCounter;
                // access to syncEvents is valid. DequeMessages() is called sequentially
                var client       = context.sendTargetClientId ? clientId : default;
                var eventMessage = MessageUtils.WriteEventMessage(syncEvents, client, seq, context.writer);
                eventMessages.Add(eventMessage);
                if (queueEvents) {
                    lock (unsentSyncEvents) {
                        sentEventMessages.AddTail(eventMessage, seq);
                    }
                }
            }
            return eventMessages.Count > 0;
        }
        
        internal void SendEventMessage(in JsonValue eventMessage) {
            var clientEvent = new ClientEvent(clientId, eventMessage);
            eventReceiver.SendEvent(clientEvent);
        }
        
        private static void CopyEventMessages(List<JsonValue> eventMessages, MessageBufferQueue<int> sentEventMessages) {
            var sumCount = 0;
            foreach (var eventMessage in sentEventMessages) {
                sumCount += eventMessage.value.Count;
            }
            var array = new byte[sumCount];
            var offset = 0;
            foreach (var eventMessage in sentEventMessages) {
                var value   = eventMessage.value;
                var copy    = new JsonValue(value, array, offset);
                offset     += value.Count;
                eventMessages.Add (copy);
            }
        }
        
        /// <summary>
        /// Remove all acknowledged serialized <see cref="EventMessage"/>' from <see cref="sentEventMessages"/>
        /// </summary>
        internal void AcknowledgeEventMessages(int eventAck) {
            lock (unsentSyncEvents) {
                while (sentEventMessages.Count > 0) {
                    var ev = sentEventMessages.GetHead();
                    if (ev.meta <= eventAck) {
                        sentEventMessages.RemoveHead();
                        continue;
                    }
                    break; 
                }
            }
        }

        /// <summary>
        /// Prepend all not acknowledged events to <see cref="unsentSyncEvents"/> in their original order
        /// and trigger sending the events stored in the deque.
        /// </summary>
        private void SendUnacknowledgedEvents() {
            lock (unsentSyncEvents) {
                if (dispatcher != null && sentEventMessages.Count > 0) {
                    resendEventMessages = true;
                    // Console.WriteLine($"unsentEventsQueue: {unsentEventsQueue.Count}");
                    // Trace.WriteLine($"*** SendUnacknowledgedEvents. Count: {unsentEventsQueue.Count}");
                    dispatcher.NewClientEvent(this);
                }
            }
        }
        
        internal void SendEvents (in SendEventsContext context) {
            var receiver = eventReceiver;
            // early out in case the target is a remote connection which already closed.
            if (receiver == null || !receiver.IsOpen()) {
                if (queueEvents)
                    return;
                lock (unsentSyncEvents) {
                    unsentSyncEvents.Clear();
                }
                return;
            }
            // Trace.WriteLine("--- SendEvents");
            while (DequeueEvents(context)) {
                // var msg = $"DequeueEvent {ev.seq}";
                // Trace.WriteLine(msg);
                // Console.WriteLine(msg);
                try {
                    // Console.WriteLine($"--- SendEvents: {events.Length}");
                    // In case the event target is remote connection it is not guaranteed that the event arrives.
                    // The remote target may already be disconnected and this is still not know when sending the event.
                    foreach (var eventMessage in context.eventMessages) {
                        var clientEvent     = new ClientEvent(clientId, eventMessage);
                        receiver.SendEvent(clientEvent);
                    }
                }
                catch (Exception e) {
                    var message = "SendEvents failed";
                    logger.Log(HubLog.Error, message, e);
                    Debug.Fail($"{message}, exception: {e}");
                }
            }
        }
    }
}