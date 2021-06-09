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
    public class Subscriber {
        internal            IMessageTarget                  messageTarget;
        internal            Subscription                    subscription;
        internal readonly   ConcurrentQueue<ChangeMessage>  queue = new ConcurrentQueue<ChangeMessage>();
        
        internal async Task SendMessages () {
            var contextPools    = new Pools(Pools.SharedPools);
            while (queue.TryDequeue(out var changeMessage)) {
                try {
                    var syncContext     = new SyncContext(contextPools, messageTarget);
                    await messageTarget.ExecuteChange(changeMessage, syncContext).ConfigureAwait(false);
                    syncContext.pools.AssertNoLeaks();
                } catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
    
    public interface IMessageTarget {
        Task ExecuteChange(ChangeMessage change, SyncContext syncContext);
    }
    
    public class MessageBroker
    {
        private readonly JsonEvaluator                          jsonEvaluator = new JsonEvaluator();
        private readonly Dictionary<IMessageTarget, Subscriber> subscribers = new Dictionary<IMessageTarget, Subscriber>();
            
        public void Subscribe (Subscription subscription, SyncContext syncContext) {
            if (subscription == null)
                return;
            var messageTarget = syncContext.messageTarget;
            if (messageTarget == null)
                return;
            var subscriber = new Subscriber {
                messageTarget   = messageTarget,
                subscription    = subscription
            };
            subscribers[messageTarget] = subscriber;
        }
        
        // todo remove - only for testing
        internal async Task SendQueueMessages() {
            foreach (var pair in subscribers) {
                var subscriber = pair.Value;
                await subscriber.SendMessages();
            }
        }

        public void EnqueueSync (SyncRequest syncRequest) {
            foreach (var pair in subscribers) {
                List<DatabaseTask>  subscriberTasks = null;
                Subscriber          subscriber = pair.Value;
                foreach (var task in syncRequest.tasks) {
                    var taskResult = FilterTask(task, subscriber.subscription);
                    if (taskResult == null)
                        continue;
                    if (subscriberTasks == null) {
                        subscriberTasks = new List<DatabaseTask>();
                    }
                    subscriberTasks.Add(taskResult);
                }
                if (subscriberTasks == null)
                    continue;
                var subscriberSync = new ChangeMessage {
                    change = subscriberTasks
                };
                subscriber.queue.Enqueue(subscriberSync);
            }
        }
        
        private DatabaseTask FilterTask (DatabaseTask task, Subscription subscription) {
            ContainerFilter containerFilter;
            switch (task.TaskType) {
                case TaskType.create:
                    var create = (CreateEntities) task;
                    containerFilter = FindFilter(subscription, create.container, TaskType.create);
                    if (containerFilter == null)
                        return null;
                    var createResult = new CreateEntities {
                        container   = create.container,
                        entities    = FilterEntities(containerFilter.filter, create.entities)
                    };
                    return createResult;
                case TaskType.update:
                    var update = (UpdateEntities) task;
                    containerFilter = FindFilter(subscription, update.container, TaskType.update);
                    if (containerFilter == null)
                        return null;
                    var updateResult = new UpdateEntities {
                        container   = update.container,
                        entities    = FilterEntities(containerFilter.filter, update.entities)
                    };
                    return updateResult;
                case TaskType.delete:
                    // todo
                    return null;
                case TaskType.patch:
                    // todo
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
        
        private static ContainerFilter FindFilter (Subscription subscription, string container, TaskType taskType) {
            foreach (var filter in subscription.filters) {
                if (filter.container == container) {
                    if (Array.IndexOf(filter.taskTypes, taskType) != -1)
                        return filter;
                    return null;
                }
            }
            return null;
        }
    }
}