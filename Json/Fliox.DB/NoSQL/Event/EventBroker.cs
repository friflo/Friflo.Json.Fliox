// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.DB.NoSQL.Event
{
    public interface IEventTarget {
        bool        IsOpen ();
        Task<bool>  ProcessEvent(DatabaseEvent ev, MessageContext messageContext);
    }
    
    public class EventBroker : IDisposable
    {
        private  readonly   JsonEvaluator                                   jsonEvaluator;
        /// key: <see cref="EventSubscriber.clientId"/>
        private  readonly   ConcurrentDictionary<string, EventSubscriber>   subscribers;
        internal readonly   bool                                            background;

        public EventBroker (bool background) {
            jsonEvaluator   = new JsonEvaluator();
            subscribers     = new ConcurrentDictionary<string, EventSubscriber>();
            this.background = background;
        }

        public void Dispose() {
            jsonEvaluator.Dispose();
        }
        
        /// used for test assertion (returned subscribers cant be manipulated)
        public ICollection<EventSubscriber> GetSubscribers() => subscribers.Values;

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
        internal void SubscribeMessage(SubscribeMessage subscribe, string clientId, IEventTarget eventTarget) {
            EventSubscriber subscriber;
            var remove = subscribe.remove;
            var prefix = Sync.SubscribeMessage.GetPrefix(subscribe.name);
            if (remove.HasValue && remove.Value) {
                if (!subscribers.TryGetValue(clientId, out subscriber))
                    return;
                if (prefix == null) {
                    subscriber.messageSubscriptions.Remove(subscribe.name);
                } else {
                    subscriber.messagePrefixSubscriptions.Remove(prefix);
                }
                RemoveEmptySubscriber(subscriber, clientId);
                return;
            }
            subscriber = GetOrCreateSubscriber(clientId, eventTarget);
            if (prefix == null) {
                subscriber.messageSubscriptions.Add(subscribe.name);
            } else {
                subscriber.messagePrefixSubscriptions.Add(prefix);
            }
        }

        internal void SubscribeChanges (SubscribeChanges subscribe, string clientId, IEventTarget eventTarget) {
            EventSubscriber subscriber;
            if (subscribe.changes.Count == 0) {
                if (!subscribers.TryGetValue(clientId, out subscriber))
                    return;
                subscriber.changeSubscriptions.Remove(subscribe.container);
                RemoveEmptySubscriber(subscriber, clientId);
                return;
            }
            subscriber = GetOrCreateSubscriber(clientId, eventTarget);
            subscriber.changeSubscriptions[subscribe.container] = subscribe;
        }
        
        private EventSubscriber GetOrCreateSubscriber(string clientId, IEventTarget eventTarget) {
            subscribers.TryGetValue(clientId, out EventSubscriber subscriber);
            if (subscriber != null)
                return subscriber;
            subscriber = new EventSubscriber(clientId, eventTarget, background);
            subscribers.TryAdd(clientId, subscriber);
            return subscriber;
        }
        
        private void RemoveEmptySubscriber(EventSubscriber subscriber, string clientId) {
            if (subscriber.SubscriptionCount > 0)
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
        
        private void ProcessSubscriber(SyncRequest syncRequest, MessageContext messageContext) {
            string  clientId = syncRequest.clientId;
            if (clientId == null)
                return;
            
            if (!subscribers.TryGetValue(clientId, out var subscriber))
                return;
            
            subscriber.UpdateTarget (messageContext.eventTarget);
            
            var eventAck = syncRequest.eventAck;
            if (!eventAck.HasValue)
                return;
            int value =  eventAck.Value;
            subscriber.AcknowledgeEvents(value);
        }
        
        private static void AddTask(ref List<DatabaseTask> tasks, DatabaseTask task) {
            if (tasks == null) {
                tasks = new List<DatabaseTask>();
            }
            tasks.Add(task);
        }

        internal void EnqueueSyncTasks (SyncRequest syncRequest, MessageContext messageContext) {
            ProcessSubscriber (syncRequest, messageContext);
            
            foreach (var pair in subscribers) {
                List<DatabaseTask>  tasks = null;
                EventSubscriber     subscriber = pair.Value;
                if (subscriber.SubscriptionCount == 0)
                    throw new InvalidOperationException("Expect SubscriptionCount > 0");
                
                // Enqueue only change events for (change) tasks which are not send by the client itself
                bool subscriberIsSender = syncRequest.clientId == subscriber.clientId;
                
                foreach (var task in syncRequest.tasks) {
                    foreach (var changesPair in subscriber.changeSubscriptions) {
                        if (subscriberIsSender)
                            continue;
                        SubscribeChanges subscribeChanges = changesPair.Value;
                        var taskResult = FilterChanges(task, subscribeChanges);
                        if (taskResult == null)
                            continue;
                        AddTask(ref tasks, taskResult);
                    }
                    if (task.TaskType == TaskType.message) {
                        var message = (SendMessage) task;
                        if (!subscriber.FilterMessage(message.name))
                            continue;
                        AddTask(ref tasks, task);
                    }
                }
                if (tasks == null)
                    continue;
                var subscriptionEvent = new SubscriptionEvent {
                    tasks       = tasks,
                    clientId    = syncRequest.clientId,
                    targetId    = subscriber.clientId
                };
                subscriber.EnqueueEvent(subscriptionEvent);
            }
        }
        
        private DatabaseTask FilterChanges (DatabaseTask task, SubscribeChanges subscribe) {
            switch (task.TaskType) {
                
                case TaskType.create:
                    if (!subscribe.changes.Contains(Change.create))
                        return null;
                    var create = (CreateEntities) task;
                    if (create.container != subscribe.container)
                        return null;
                    var createResult = new CreateEntities {
                        container   = create.container,
                        entities    = FilterEntities(subscribe.filter, create.entities),
                        keyName     = create.keyName   
                    };
                    return createResult;
                
                case TaskType.upsert:
                    if (!subscribe.changes.Contains(Change.upsert))
                        return null;
                    var upsert = (UpsertEntities) task;
                    if (upsert.container != subscribe.container)
                        return null;
                    var upsertResult = new UpsertEntities {
                        container   = upsert.container,
                        entities    = FilterEntities(subscribe.filter, upsert.entities),
                        keyName     = upsert.keyName
                    };
                    return upsertResult;
                
                case TaskType.delete:
                    if (!subscribe.changes.Contains(Change.delete))
                        return null;
                    var delete = (DeleteEntities) task;
                    if (subscribe.container != delete.container)
                        return null;
                    // todo apply filter
                    return task;
                
                case TaskType.patch:
                    if (!subscribe.changes.Contains(Change.patch))
                        return null;
                    var patch = (PatchEntities) task;
                    if (subscribe.container != patch.container)
                        return null;
                    // todo apply filter
                    return task;
                
                default:
                    return null;
            }
        }
        
        private List<JsonValue> FilterEntities (FilterOperation filter, List<JsonValue> entities)    
        {
            if (filter == null)
                return entities;
            var jsonFilter      = new JsonFilter(filter); // filter can be reused
            var result          = new List<JsonValue>();

            for (int n = 0; n < entities.Count; n++) {
                var value   = entities[n];
                if (jsonEvaluator.Filter(value.json, jsonFilter)) {
                    result.Add(value);
                }
            }
            return result;
        }
    }
}