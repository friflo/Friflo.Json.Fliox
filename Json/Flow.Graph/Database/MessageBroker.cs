// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Database
{
    public interface IEventTarget {
        bool        IsOpen ();
        Task<bool>  SendEvent(DatabaseEvent ev, SyncContext syncContext);
    }
    
    public class MessageBroker : IDisposable
    {
        private readonly JsonEvaluator                              jsonEvaluator = new JsonEvaluator();
        private readonly Dictionary<IEventTarget, EventSubscriber>  subscribers = new Dictionary<IEventTarget, EventSubscriber>();

        public void Dispose() {
            jsonEvaluator.Dispose();
        }

        public void Subscribe (SubscribeChanges subscribe, IEventTarget eventTarget) {
            if (!subscribers.TryGetValue(eventTarget, out var messageSubscriber)) {
                messageSubscriber = new EventSubscriber(eventTarget);
                subscribers.Add(eventTarget, messageSubscriber);
            }
            messageSubscriber.subscribeMap[subscribe.container] = subscribe;
        }
        
        // todo remove - only for testing
        internal async Task SendQueueMessages() {
            foreach (var pair in subscribers) {
                var subscriber = pair.Value;
                await subscriber.SendMessages().ConfigureAwait(false);
            }
        }

        public void EnqueueSyncTasks (SyncRequest syncRequest) {
            foreach (var pair in subscribers) {
                List<DatabaseTask>  tasks = null;
                EventSubscriber     subscriber = pair.Value;
                foreach (var task in syncRequest.tasks) {
                    foreach (var messagePair in subscriber.subscribeMap) {
                        SubscribeChanges subscribeChanges = messagePair.Value;
                        var taskResult = FilterTask(task, subscribeChanges);
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
                var changesEvent = new ChangesEvent {tasks = tasks};
                subscriber.queue.Enqueue(changesEvent);
            }
        }
        
        private DatabaseTask FilterTask (DatabaseTask task, SubscribeChanges subscribe) {
            switch (task.TaskType) {
                case TaskType.create:
                    var create = (CreateEntities) task;
                    if (!MatchFilter(subscribe, create.container, TaskType.create))
                        return null;
                    var createResult = new CreateEntities {
                        container   = create.container,
                        entities    = FilterEntities(subscribe.filter, create.entities)
                    };
                    return createResult;
                case TaskType.update:
                    var update = (UpdateEntities) task;
                    if (!MatchFilter(subscribe, update.container, TaskType.create))
                        return null;
                    var updateResult = new UpdateEntities {
                        container   = update.container,
                        entities    = FilterEntities(subscribe.filter, update.entities)
                    };
                    return updateResult;
                case TaskType.delete:
                    // todo apply filter
                    var delete = (DeleteEntities) task;
                    if (subscribe.container == delete.container)
                        return task;
                    return null;
                case TaskType.patch:
                    // todo apply filter
                    var patch = (PatchEntities) task;
                    if (subscribe.container == patch.container)
                        return task;
                    return null;
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
        
        private static bool MatchFilter (SubscribeChanges subscribe, string container, TaskType taskType) {
            if (subscribe.container == container) {
                if (subscribe.types.Contains(taskType))
                    return true;
                return false;
            }
            return false;
        }
    }
}