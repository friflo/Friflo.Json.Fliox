// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    /// <summary>
    /// Contain all subscriptions to entity changes and messages for a specific <see cref="database"/>. <br/>
    /// A <see cref="EventSubClient"/> has a single <see cref="DatabaseSubs"/> instance for each database.
    /// </summary>
    internal sealed class DatabaseSubs
    {
        private  readonly   string                          database;

        public   override   string                          ToString()          => $"database: {database}";

        private  readonly   HashSet<string>                 messageSubs         = new HashSet<string>();
        private  readonly   HashSet<string>                 messagePrefixSubs   = new HashSet<string>();
        /// key: <see cref="SubscribeChanges.container"/>
        [DebuggerBrowsable(Never)] 
        private  readonly   Dictionary<string, ChangeSub>   changeSubs          = new Dictionary<string, ChangeSub>();
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             IReadOnlyCollection<ChangeSub>  ChangeSubs          => changeSubs.Values;

        internal            int                             SubCount => changeSubs.Count + messageSubs.Count + messagePrefixSubs.Count; 

        
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
                ChangeSub sub = pair.Value;
                var changeSubscription = new ChangeSubscription {
                    container   = sub.container,
                    changes     = sub.changes,
                    filter      = sub.jsonFilter?.Linq
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
            var         filter      = subscribe.filter;
            JsonFilter  jsonFilter  = null;
            if (filter != null) {
                var operation = Operation.Parse("o=>" + filter, out var parseError);
                if (operation == null)
                    throw new InvalidOperationException($"invalid filter {filter} - {parseError}");
                if (!(operation is FilterOperation filterOperation))
                    throw new InvalidOperationException($"filter {filter} is not a FilterOperation");
                jsonFilter      = filterOperation.IsTrue ? null : new JsonFilter(filterOperation);
            }
            var changeSub       = new ChangeSub(subscribe.container, subscribe.changes, jsonFilter);
            changeSubs[subscribe.container] = changeSub;
        }

        internal void AddEventTasks(
            List<SyncRequestTask>       tasks,
            EventSubClient              subClient,
            ref List<SyncRequestTask>   eventTasks,
            JsonEvaluator               jsonEvaluator)
        {
            foreach (var task in tasks) {
                switch (task.TaskType) {
                    case TaskType.create:
                    case TaskType.upsert:
                    case TaskType.delete:
                    case TaskType.patch:
                        foreach (var pair in changeSubs) {
                            ChangeSub changeSub = pair.Value;
                            var taskResult = FilterUtils.FilterChanges(task, changeSub, jsonEvaluator);
                            if (taskResult == null)
                                continue;
                            AddTask(ref eventTasks, taskResult);
                        }
                        break;
                    case TaskType.message:
                    case TaskType.command:
                        var messageTask = (SyncMessageTask)task;
                        if (!IsEventTarget(subClient, messageTask))
                            continue;
                        if (!FilterMessage(messageTask.name))
                            continue;
                        // don't leak userId's & clientId's to subscribed clients
                        messageTask.users   = null;
                        messageTask.clients = null;
                        messageTask.groups  = null;
                        AddTask(ref eventTasks, messageTask);
                    break;
                }
            }
        }
        
        private static void AddTask(ref List<SyncRequestTask> tasks, SyncRequestTask task) {
            if (tasks == null) {
                tasks = new List<SyncRequestTask>();
            }
            tasks.Add(task);
        }
        
        private static bool IsEventTarget (EventSubClient subClient, SyncMessageTask messageTask)
        {
            var isEventTarget   = true;
            // --- is event subscriber a target client
            var targetClients   = messageTask.clients;
            if (targetClients != null) {
                foreach (var targetClient in targetClients) {
                    if (subClient.clientId.IsEqual(targetClient))
                        return true;
                }
                isEventTarget = false;
            }
            var subUser = subClient.user;
            // --- is event subscriber a target user
            var targetUsers     = messageTask.users;
            if (targetUsers != null) {
                foreach (var targetUser in targetUsers) {
                    if (subUser.userId.IsEqual(targetUser))
                        return true;
                }
                isEventTarget = false;
            }
            // --- is event subscriber a target group
            var targetGroups    = messageTask.groups;
            if (targetGroups != null) {
                var userGroups = subUser.groups;
                foreach (var targetGroup in targetGroups) {
                    if (userGroups.Contains(targetGroup))
                        return true;
                }
                isEventTarget = false;
            }
            return isEventTarget;
        }
    }
    
    internal readonly struct ChangeSub {
        internal    readonly    string          container;
        internal    readonly    EntityChange[]  changes;
        internal    readonly    JsonFilter      jsonFilter;
        
        internal ChangeSub(string container, EntityChange[] changes, JsonFilter jsonFilter) {
            this.container  = container;
            this.changes    = changes;
            this.jsonFilter = jsonFilter;
        }
    }
}