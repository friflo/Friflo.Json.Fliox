// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Database
{
    public class ChangesSubscriber {
        private  readonly   IMessageTarget                  messageTarget;
        internal readonly   SubscribeChanges                subscribe;
        internal readonly   ConcurrentQueue<ChangesMessage> queue = new ConcurrentQueue<ChangesMessage>();
        
        public ChangesSubscriber (IMessageTarget messageTarget, SubscribeChanges subscribe) {
            this.messageTarget  = messageTarget;
            this.subscribe      = subscribe;
        }
        
        internal async Task SendChangeMessages () {
            if (!messageTarget.IsOpen())
                return;
            
            var contextPools    = new Pools(Pools.SharedPools);
            while (queue.TryPeek(out var changeMessage)) {
                try {
                    var syncContext     = new SyncContext(contextPools, messageTarget);
                    var success = await messageTarget.ExecuteChange(changeMessage, syncContext).ConfigureAwait(false);
                    if (success) {
                        queue.TryDequeue(out _);
                    }
                    syncContext.pools.AssertNoLeaks();
                } catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
    
    public interface IMessageTarget {
        bool        IsOpen ();
        Task<bool>  ExecuteChange(ChangesMessage change, SyncContext syncContext);
    }
    
    public class MessageBroker
    {
        private readonly JsonEvaluator                                  jsonEvaluator = new JsonEvaluator();
        private readonly Dictionary<IMessageTarget, ChangesSubscriber>  subscribers = new Dictionary<IMessageTarget, ChangesSubscriber>();
            
        public void Subscribe (SubscribeChanges subscribe, IMessageTarget messageTarget) {
            var filters = subscribe.filters;
            if (filters == null || filters.Count == 0) {
                subscribers.Remove(messageTarget);
                return;
            }
            var subscriber = new ChangesSubscriber (messageTarget, subscribe);
            subscribers[messageTarget] = subscriber;
        }
        
        // todo remove - only for testing
        internal async Task SendQueueMessages() {
            foreach (var pair in subscribers) {
                var subscriber = pair.Value;
                await subscriber.SendChangeMessages();
            }
        }

        public void EnqueueSyncTasks (SyncRequest syncRequest) {
            foreach (var pair in subscribers) {
                List<DatabaseTask>  changes = null;
                ChangesSubscriber   subscriber = pair.Value;
                foreach (var task in syncRequest.tasks) {
                    var taskResult = FilterTask(task, subscriber.subscribe);
                    if (taskResult == null)
                        continue;
                    if (changes == null) {
                        changes = new List<DatabaseTask>();
                    }
                    changes.Add(taskResult);
                }
                if (changes == null)
                    continue;
                var subscriberSync = new ChangesMessage {changes = changes};
                subscriber.queue.Enqueue(subscriberSync);
            }
        }
        
        private DatabaseTask FilterTask (DatabaseTask task, SubscribeChanges subscribe) {
            ContainerFilter containerFilter;
            switch (task.TaskType) {
                case TaskType.create:
                    var create = (CreateEntities) task;
                    containerFilter = FindFilter(subscribe, create.container, TaskType.create);
                    if (containerFilter == null)
                        return null;
                    var createResult = new CreateEntities {
                        container   = create.container,
                        entities    = FilterEntities(containerFilter.filter, create.entities)
                    };
                    return createResult;
                case TaskType.update:
                    var update = (UpdateEntities) task;
                    containerFilter = FindFilter(subscribe, update.container, TaskType.update);
                    if (containerFilter == null)
                        return null;
                    var updateResult = new UpdateEntities {
                        container   = update.container,
                        entities    = FilterEntities(containerFilter.filter, update.entities)
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
        
        private static ContainerFilter FindFilter (SubscribeChanges subscribe, string container, TaskType taskType) {
            foreach (var filter in subscribe.filters) {
                if (filter.container == container) {
                    if (Array.IndexOf(filter.changes, taskType) != -1)
                        return filter;
                    return null;
                }
            }
            return null;
        }
    }
}