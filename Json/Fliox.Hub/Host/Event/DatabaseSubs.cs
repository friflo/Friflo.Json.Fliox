// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal class DatabaseSubs
    {
        private  readonly   string                                  database;

        public   override   string                                  ToString() => database;

        /// key: <see cref="SubscribeChanges.container"/>
        private  readonly   Dictionary<string, SubscribeChanges>    changeSubs         = new Dictionary<string, SubscribeChanges>();
        private  readonly   HashSet<string>                         messageSubs        = new HashSet<string>();
        private  readonly   HashSet<string>                         messagePrefixSubs  = new HashSet<string>();
        
        internal            int                                     SubCount => changeSubs.Count + messageSubs.Count + messagePrefixSubs.Count; 

        
        internal DatabaseSubs (string database) {
            this.database   = database;
        }
        
        private bool FilterMessage (string messageName) {
            if (messageSubs.Contains(messageName))
                return true;
            foreach (var prefixSub in messagePrefixSubs) {
                if (messageName.StartsWith(prefixSub)) {
                    return true;
                }
            }
            return false;
        }
        
        internal List<string> GetMessageSubscriptions (List<string> msgSubs) {
            foreach (var messageSub in messageSubs) {
                if (msgSubs == null) msgSubs = new List<string>();
                msgSubs.Add(messageSub);
            }
            foreach (var messageSub in messagePrefixSubs) {
                if (msgSubs == null) msgSubs = new List<string>();
                msgSubs.Add(messageSub + "*");
            }
            return msgSubs;
        }
        
        internal List<ChangeSubscription> GetChangeSubscriptions (List<ChangeSubscription> subs) {
            if (changeSubs.Count == 0)
                return null;
            if (subs == null) subs = new List<ChangeSubscription>(changeSubs.Count);
            subs.Clear();
            subs.Capacity = changeSubs.Count;
            foreach (var pair in changeSubs) {
                SubscribeChanges sub = pair.Value;
                var changeSubscription = new ChangeSubscription {
                    container   = sub.container,
                    changes     = sub.changes,
                    filter      = sub.filterOp?.Linq
                };
                subs.Add(changeSubscription);
            }
            return subs;
        }

        internal void RemoveMessageSubscription(string name) {
            var prefix = SubscribeMessage.GetPrefix(name);
            if (prefix == null) {
                messageSubs.Remove(name);
            } else {
                messagePrefixSubs.Remove(prefix);
            }
        }

        internal void AddMessageSubscription(string name) {
            var prefix = SubscribeMessage.GetPrefix(name);
            if (prefix == null) {
                messageSubs.Add(name);
            } else {
                messagePrefixSubs.Add(prefix);
            }
        }

        internal void RemoveChangeSubscription(string container) {
            changeSubs.Remove(container);
        }

        internal void AddChangeSubscription(SubscribeChanges subscribe) {
            changeSubs[subscribe.container] = subscribe;
        }

        internal void AddEventTasks(
            SyncRequest                 syncRequest,
            EventSubscriber             eventSubscriber,
            bool                        subscriberIsSender,
            ref List<SyncRequestTask>   eventTasks,
            JsonEvaluator               jsonEvaluator)
        {
            foreach (var task in syncRequest.tasks) {
                foreach (var changesPair in changeSubs) {
                    if (subscriberIsSender)
                        continue;
                    SubscribeChanges subscribeChanges = changesPair.Value;
                    var taskResult = FilterUtils.FilterChanges(task, subscribeChanges, jsonEvaluator);
                    if (taskResult == null)
                        continue;
                    AddTask(ref eventTasks, taskResult);
                }
                if (task is SyncMessageTask messageTask) {
                    if (!IsEventTarget(eventSubscriber, messageTask))
                        continue;
                    if (!FilterMessage(messageTask.name))
                        continue;
                    AddTask(ref eventTasks, task);
                }
            }
        }
        
        private static void AddTask(ref List<SyncRequestTask> tasks, SyncRequestTask task) {
            if (tasks == null) {
                tasks = new List<SyncRequestTask>();
            }
            tasks.Add(task);
        }
        
        private static bool IsEventTarget (EventSubscriber eventSubscriber, SyncMessageTask messageTask) {
            var clientId        = eventSubscriber.clientId;
            var targetClients   = messageTask.targetClients;
            var isEventTarget   = true;
            if (targetClients != null) {
                foreach (var targetClient in targetClients) {
                    if (clientId.IsEqual(targetClient))
                        return true;
                }
                isEventTarget = false;
            }
            var userId          = eventSubscriber.user.userId;
            var targetUsers     = messageTask.targetUsers;
            if (targetUsers != null) {
                foreach (var targetUser in targetUsers) {
                    if (userId.IsEqual(targetUser))
                        return true;
                }
                isEventTarget = false;
            }
            return isEventTarget;
        }
    }
}