// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    public interface IEventReceiver {
        bool        IsOpen ();
        Task<bool>  ProcessEvent(ProtocolEvent ev);
    }
    
    /// <summary>
    /// An <see cref="EventDispatcher"/> is used to enable Pub-Sub. <br/>
    /// If assigned to <see cref="FlioxHub.EventDispatcher"/> the <see cref="FlioxHub"/> send
    /// push events to clients for database changes and messages these clients have subscribed. <br/>
    /// In case of remote database connections <b>WebSockets</b> are used to send push events to clients.   
    /// </summary>
    public sealed class EventDispatcher : IDisposable
    {
        private  readonly   SharedEnv                                       sharedEnv;
        private  readonly   JsonEvaluator                                   jsonEvaluator;
        /// key: <see cref="EventSubscriber.clientId"/>
        [DebuggerBrowsable(Never)] 
        private  readonly   ConcurrentDictionary<JsonKey, EventSubscriber>  subscribers;
        /// expose <see cref="subscribers"/> as property to show them as list in Debugger
        // ReSharper disable once UnusedMember.Local
        private             ICollection<EventSubscriber>                    Subscribers => subscribers.Values;
        internal readonly   bool                                            background;

        public   override   string                                          ToString() => $"subscribers: {subscribers.Count}";

        private const string MissingEventReceiver = "subscribing events requires an eventReceiver. E.g a WebSocket as a target for push events.";

        public EventDispatcher (bool background, SharedEnv env = null) {
            sharedEnv       = env ?? SharedEnv.Default;
            jsonEvaluator   = new JsonEvaluator();
            subscribers     = new ConcurrentDictionary<JsonKey, EventSubscriber>(JsonKey.Equality);
            this.background = background;
        }

        public void Dispose() {
            jsonEvaluator.Dispose();
        }
        
        internal bool TryGetSubscriber(JsonKey key, out EventSubscriber subscriber) {
            return subscribers.TryGetValue(key, out subscriber);
        }
        
        /// used for test assertion
        public int NotAcknowledgedEvents() {
            int count = 0;
            foreach (var subscriber in subscribers) {
                count += subscriber.Value.SentEventsCount;
            }
            return count;
        }

        public async Task FinishQueues() {
            if (!background)
                return;
            var loopTasks = new List<Task>();
            foreach (var pair in subscribers) {
                var subscriber = pair.Value;
                subscriber.FinishQueue();
                loopTasks.Add(subscriber.triggerLoop);
            }
            await Task.WhenAll(loopTasks).ConfigureAwait(false);
        }
        
        // -------------------------------- add / remove subscriptions --------------------------------
        internal bool SubscribeMessage(string database, SubscribeMessage subscribe, in JsonKey clientId, IEventReceiver eventReceiver, out string error) {
            if (eventReceiver == null) {
                error = MissingEventReceiver; 
                return false;
            }
            error = null;
            EventSubscriber subscriber;
            var remove = subscribe.remove;
            if (remove.HasValue && remove.Value) {
                if (!subscribers.TryGetValue(clientId, out subscriber))
                    return true;
                if (!subscriber.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    return true;
                }
                databaseSubs.RemoveMessageSubscription(subscribe.name);
                RemoveEmptySubscriber(subscriber, clientId);
                return true;
            } else {
                subscriber = GetOrCreateSubscriber(clientId, eventReceiver);
                if (!subscriber.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs(database);
                    subscriber.databaseSubs.Add(database, databaseSubs);
                }
                databaseSubs.AddMessageSubscription(subscribe.name);
                return true;
            }
        }

        internal bool SubscribeChanges (string database, SubscribeChanges subscribe, in JsonKey clientId, IEventReceiver eventReceiver, out string error) {
            if (eventReceiver == null) {
                error = MissingEventReceiver; 
                return false;
            }
            error = null;
            EventSubscriber subscriber;
            if (subscribe.changes.Count == 0) {
                if (!subscribers.TryGetValue(clientId, out subscriber))
                    return true;
                if (!subscriber.databaseSubs.TryGetValue(database, out var databaseSubs))
                    return true;
                databaseSubs.RemoveChangeSubscription(subscribe.container);
                RemoveEmptySubscriber(subscriber, clientId);
                return true;
            } else {
                subscriber = GetOrCreateSubscriber(clientId, eventReceiver);
                if (!subscriber.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs(database);
                    subscriber.databaseSubs.Add(database, databaseSubs);
                }
                databaseSubs.AddChangeSubscription(subscribe);
                return true;
            }
        }
        
        private EventSubscriber GetOrCreateSubscriber(in JsonKey clientId, IEventReceiver eventReceiver) {
            subscribers.TryGetValue(clientId, out EventSubscriber subscriber);
            if (subscriber != null)
                return subscriber;
            subscriber = new EventSubscriber(sharedEnv, clientId, eventReceiver, background);
            subscribers.TryAdd(clientId, subscriber);
            return subscriber;
        }
        
        private void RemoveEmptySubscriber(EventSubscriber subscriber, in JsonKey clientId) {
            if (subscriber.SubCount > 0)
                return;
            subscribers.TryRemove(clientId, out _);
        }
        
        
        // -------------------------- event distribution --------------------------------
        // use only for testing
        internal async Task SendQueuedEvents() {
            if (background) {
                throw new InvalidOperationException("must not be called, if using a background Tasks");
            }
            foreach (var pair in subscribers) {
                var subscriber = pair.Value;
                await subscriber.SendEvents().ConfigureAwait(false);
            }
        }
        
        private void ProcessSubscriber(SyncRequest syncRequest, SyncContext syncContext) {
            ref JsonKey  clientId = ref syncContext.clientId;
            if (clientId.IsNull())
                return;
            
            if (!subscribers.TryGetValue(clientId, out var subscriber))
                return;
            var eventReceiver = syncContext.eventReceiver;
            if (eventReceiver != null) {
                subscriber.UpdateTarget (eventReceiver);
            }
            
            var eventAck = syncRequest.eventAck;
            if (!eventAck.HasValue)
                return;
            int value =  eventAck.Value;
            subscriber.AcknowledgeEvents(value);
        }
        
        internal void EnqueueSyncTasks (SyncRequest syncRequest, SyncContext syncContext) {
            var database    = syncContext.DatabaseName;
            ProcessSubscriber (syncRequest, syncContext);
            
            using (var pooled = syncContext.ObjectMapper.Get()) {
                ObjectWriter writer     = pooled.instance.writer;
                writer.Pretty           = false;    // write sub's as one liner
                writer.WriteNullMembers = false;
                foreach (var pair in subscribers) {
                    List<SyncRequestTask>  eventTasks = null;
                    EventSubscriber     subscriber = pair.Value;
                    if (subscriber.SubCount == 0)
                        throw new InvalidOperationException("Expect SubscriptionCount > 0");
                    
                    if (!subscriber.databaseSubs.TryGetValue(database, out var databaseSubs))
                        continue;
                    
                    // Enqueue only change events for (change) tasks which are not send by the client itself
                    bool subscriberIsSender = syncContext.clientId.IsEqual(subscriber.clientId);
                    databaseSubs.AddEventTasks(syncRequest, subscriberIsSender, ref eventTasks, jsonEvaluator);

                    if (eventTasks == null)
                        continue;
                    var eventMessage = new EventMessage {
                        tasks       = eventTasks.ToArray(),
                        srcUserId   = syncRequest.userId,
                        dstClientId = subscriber.clientId
                    };
                    if (SerializeRemoteEvents && subscriber.IsRemoteTarget) {
                        SerializeRemoteEvent(eventMessage, eventTasks, writer);
                    }
                    subscriber.EnqueueEvent(eventMessage);
                }
            }
        }
        
        private static bool SerializeRemoteEvents = true; // set to false for development

        /// Optimization: For remote connections the tasks are serialized to <see cref="EventMessage.tasksJson"/>.
        /// Benefits of doing this:
        /// - serialize a task only once for multiple targets
        /// - storing only a single byte[] for a task instead of a complex SyncRequestTask which is not used anymore
        private static void SerializeRemoteEvent(EventMessage eventMessage, List<SyncRequestTask> tasks, ObjectWriter writer) {
            var tasksJson = new JsonValue [tasks.Count];
            eventMessage.tasksJson = tasksJson;
            for (int n = 0; n < tasks.Count; n++) {
                var task = tasks[n];
                if (task.json == null) {
                    task.json = new JsonValue(writer.WriteAsArray(task));
                }
                tasksJson[n] = task.json.Value;
            }
            tasks.Clear();
            eventMessage.tasks = null;
        }
    }
}