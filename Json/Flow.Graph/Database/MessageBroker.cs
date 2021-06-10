// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Database
{
    public interface IMessageTarget {
        bool        IsOpen ();
        Task<bool>  SendMessage(PushMessage message, SyncContext syncContext);
    }
    
    public class MessageBroker : IDisposable
    {
        private readonly JsonEvaluator                                  jsonEvaluator = new JsonEvaluator();
        private readonly Dictionary<IMessageTarget, MessageSubscriber>  subscribers = new Dictionary<IMessageTarget, MessageSubscriber>();

        public void Dispose() {
            jsonEvaluator.Dispose();
        }

        public void Subscribe (SubscribeMessages subscribe, IMessageTarget messageTarget) {
            var filters = subscribe.filters;
            if (filters == null || filters.Count == 0) {
                subscribers.Remove(messageTarget);
                return;
            }
            var subscriber = new MessageSubscriber (messageTarget, subscribe);
            subscribers[messageTarget] = subscriber;
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
                MessageSubscriber   subscriber = pair.Value;
                foreach (var task in syncRequest.tasks) {
                    var taskResult = FilterTask(task, subscriber.subscribe);
                    if (taskResult == null)
                        continue;
                    if (tasks == null) {
                        tasks = new List<DatabaseTask>();
                    }
                    tasks.Add(taskResult);
                }
                if (tasks == null)
                    continue;
                var subscriberSync = new DatabaseMessage {tasks = tasks};
                subscriber.queue.Enqueue(subscriberSync);
            }
        }
        
        private DatabaseTask FilterTask (DatabaseTask task, SubscribeMessages subscribe) {
            MessageFilter messageFilter;
            switch (task.TaskType) {
                case TaskType.create:
                    var create = (CreateEntities) task;
                    messageFilter = FindFilter(subscribe, create.container, TaskType.create);
                    if (messageFilter == null)
                        return null;
                    var createResult = new CreateEntities {
                        container   = create.container,
                        entities    = FilterEntities(messageFilter.filter, create.entities)
                    };
                    return createResult;
                case TaskType.update:
                    var update = (UpdateEntities) task;
                    messageFilter = FindFilter(subscribe, update.container, TaskType.update);
                    if (messageFilter == null)
                        return null;
                    var updateResult = new UpdateEntities {
                        container   = update.container,
                        entities    = FilterEntities(messageFilter.filter, update.entities)
                    };
                    return updateResult;
                case TaskType.delete:
                    // todo apply filter
                    return task;
                case TaskType.patch:
                    // todo apply filter
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
        
        private static MessageFilter FindFilter (SubscribeMessages subscribe, string container, TaskType taskType) {
            foreach (var filter in subscribe.filters) {
                if (filter.container == container) {
                    if (Array.IndexOf(filter.types, taskType) != -1)
                        return filter;
                    return null;
                }
            }
            return null;
        }
    }
}