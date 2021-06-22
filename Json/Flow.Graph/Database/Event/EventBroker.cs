// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Database.Event
{
    public interface IEventTarget {
        bool        IsOpen ();
        Task<bool>  ProcessEvent(DatabaseEvent ev, SyncContext syncContext);
    }
    
    public class EventBroker : IDisposable
    {
        private  readonly   JsonEvaluator                                   jsonEvaluator;
        /// key: <see cref="EventSubscriber.clientId"/>
        private  readonly   ConcurrentDictionary<string, EventSubscriber>   subscribers;
        public   readonly   bool                                            background;

        public EventBroker (bool background) {
            jsonEvaluator   = new JsonEvaluator();
            subscribers     = new ConcurrentDictionary<string, EventSubscriber>();
            this.background = background;
        }

        public void Dispose() {
            jsonEvaluator.Dispose();
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

        public void Subscribe (SubscribeChanges subscribe, string clientId, IEventTarget eventTarget) {
            EventSubscriber eventSubscriber;
            if (subscribe.changes.Count == 0) {
                if (!subscribers.TryGetValue(clientId, out eventSubscriber))
                    return;
                var subscriptions = eventSubscriber.subscriptions;
                subscriptions.Remove(subscribe.container);
                if (subscriptions.Count > 0)
                    return;
                // remove subscriber - nothing is subscribed
                subscribers.TryRemove(clientId, out _);
                return;
            }
            subscribers.TryGetValue(clientId, out eventSubscriber);
            if (eventSubscriber == null) {
                eventSubscriber = new EventSubscriber(clientId, eventTarget, background);
                subscribers.TryAdd(clientId, eventSubscriber);
            }
            eventSubscriber.subscriptions[subscribe.container] = subscribe;
        }
        
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
            string  clientId = syncRequest.clientId;
            if (clientId == null)
                return;
            
            if (!subscribers.TryGetValue(clientId, out var subscriber))
                return;
            
            subscriber.UpdateTarget (syncContext.eventTarget);
            
            var eventAck = syncRequest.eventAck;
            if (!eventAck.HasValue)
                return;
            int value =  eventAck.Value;
            subscriber.AcknowledgeEvents(value);
        }

        public void EnqueueSyncTasks (SyncRequest syncRequest, SyncContext syncContext) {
            ProcessSubscriber (syncRequest, syncContext);
            
            foreach (var pair in subscribers) {
                List<DatabaseTask>  tasks = null;
                EventSubscriber     subscriber = pair.Value;
                if (subscriber.subscriptions.Count == 0)
                    throw new InvalidOperationException("Expect subscribeMap not empty");
                
                // Enqueue only change events for (change) tasks which are not send by the client itself
                bool subscriberIsSender = syncRequest.clientId == subscriber.clientId;
                
                foreach (var task in syncRequest.tasks) {
                    foreach (var changesPair in subscriber.subscriptions) {
                        SubscribeChanges subscribeChanges = changesPair.Value;
                        var taskResult = FilterTask(task, subscribeChanges, subscriberIsSender);
                        if (taskResult == null)
                            continue;
                        if (tasks == null) {
                            tasks = new List<DatabaseTask>();
                        }
                        tasks.Add(taskResult);
                    }
                }
                if (tasks == null)
                    continue;
                var changeEvent = new ChangeEvent {
                    tasks       = tasks,
                    clientId    = syncRequest.clientId,
                    targetId    = subscriber.clientId
                };
                subscriber.EnqueueEvent(changeEvent);
            }
        }
        
        private DatabaseTask FilterTask (DatabaseTask task, SubscribeChanges subscribe, bool subscriberIsSender) {
            switch (task.TaskType) {
                
                case TaskType.create:
                    if (subscriberIsSender)
                        return null;
                    if (!subscribe.changes.Contains(Change.create))
                        return null;
                    var create = (CreateEntities) task;
                    if (create.container != subscribe.container)
                        return null;
                    var createResult = new CreateEntities {
                        container   = create.container,
                        entities    = FilterEntities(subscribe.filter, create.entities)
                    };
                    return createResult;
                
                case TaskType.update:
                    if (subscriberIsSender)
                        return null;
                    if (!subscribe.changes.Contains(Change.update))
                        return null;
                    var update = (UpdateEntities) task;
                    if (update.container != subscribe.container)
                        return null;
                    var updateResult = new UpdateEntities {
                        container   = update.container,
                        entities    = FilterEntities(subscribe.filter, update.entities)
                    };
                    return updateResult;
                
                case TaskType.delete:
                    if (subscriberIsSender)
                        return null;
                    if (!subscribe.changes.Contains(Change.delete))
                        return null;
                    var delete = (DeleteEntities) task;
                    if (subscribe.container != delete.container)
                        return null;
                    // todo apply filter
                    return task;
                
                case TaskType.patch:
                    if (subscriberIsSender)
                        return null;
                    if (!subscribe.changes.Contains(Change.patch))
                        return null;
                    var patch = (PatchEntities) task;
                    if (subscribe.container != patch.container)
                        return null;
                    // todo apply filter
                    return task;
                
                case TaskType.echo:
                    if (!subscribe.changes.Contains(Change.echo))
                        return null;
                    return task;
                
                default:
                    return null;
            }
        }
        
        private Dictionary<string, EntityValue> FilterEntities (FilterOperation filter, Dictionary<string, EntityValue> entities) {
            if (filter == null)
                return entities;
            var jsonFilter      = new JsonFilter(filter); // filter can be reused
            var result          = new Dictionary<string, EntityValue>();

            foreach (var entityPair in entities) {
                string      key     = entityPair.Key;
                EntityValue value   = entityPair.Value;
                var         payload = value.Json;
                if (jsonEvaluator.Filter(payload, jsonFilter)) {
                    result.Add(key, value);
                }
            }
            return result;
        }
    }
}