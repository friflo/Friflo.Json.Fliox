// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    /// <summary>
    /// Contain all subscriptions to entity changes and messages for a specific database. <br/>
    /// A <see cref="EventSubClient"/> has a single <see cref="DatabaseSubs"/> instance for each database.
    /// </summary>
    internal sealed class DatabaseSubs
    {
        private  readonly   HashSet<string>     messageSubs         = new HashSet<string>();
        private  readonly   HashSet<string>     messagePrefixSubs   = new HashSet<string>();
        /// key: <see cref="SubscribeChanges.container"/> - used array instead of Dictionary for performance
        internal            ChangeSub[]         changeSubs          = Array.Empty<ChangeSub>();

        internal            int                 SubCount => changeSubs.Length + messageSubs.Count + messagePrefixSubs.Count;
        
        internal static readonly  DatabaseSubsComparer Equality = new DatabaseSubsComparer();

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
            var changeSubsLength = changeSubs.Length;
            if (changeSubsLength == 0)
                return null;
            if (subs == null) subs = new List<ChangeSubscription>(changeSubsLength);
            subs.Clear();
            subs.Capacity = changeSubsLength;
            foreach (var sub in changeSubs) {
                var changeSubscription = new ChangeSubscription {
                    container   = sub.container.value,
                    changes     = EntityChangeUtils.FlagsToList(sub.changes),
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
            var list = new List<ChangeSub>(changeSubs.Length);
            foreach (var changeSub in changeSubs) {
                if (changeSub.container.value == container)
                    continue;
                list.Add(changeSub);
            }
            if (changeSubs.Length == list.Count)
                return;
            changeSubs = list.ToArray();
        }

        internal void AddChangeSubscription(SubscribeChanges subscribe) {
            var         filter      = subscribe.filter;
            JsonFilter  jsonFilter  = null;
            if (filter != null) {
                var operation = Operation.Parse(filter, out var parseError);
                if (operation == null)
                    throw new InvalidOperationException($"invalid filter {filter} - {parseError}");
                if (!(operation is FilterOperation filterOperation))
                    throw new InvalidOperationException($"filter {filter} is not a FilterOperation");
                jsonFilter      = filterOperation.IsTrue ? null : new JsonFilter(filterOperation);
            }
            var changeSub   = new ChangeSub(subscribe.container, subscribe.changes, jsonFilter, filter);
            
            // remove old change subscription if exist and add new one
            RemoveChangeSubscription(subscribe.container);
            var oldLen          = changeSubs.Length;
            var newChangeSubs   = new ChangeSub[oldLen + 1];
            Array.Copy(changeSubs, newChangeSubs, oldLen);
            changeSubs          = newChangeSubs;
            changeSubs[oldLen]  = changeSub;
        }

        /// <summary>
        /// Create <paramref name="eventTasks"/> for all <paramref name="tasks"/> the <paramref name="subClient"/> subscribed.<br/> 
        /// Return true if <paramref name="eventTasks"/> were created. Otherwise false.
        /// </summary>
        internal  bool CreateEventTasks(
            List<SyncRequestTask>       tasks,
            EventSubClient              subClient,
            ref List<SyncRequestTask>   eventTasks,
            JsonEvaluator               jsonEvaluator)
        {
            var tasksAdded = false;
            foreach (var task in tasks) {
                switch (task.TaskType) {
                    case TaskType.create:
                    case TaskType.upsert:
                    case TaskType.delete:
                    case TaskType.merge: {
                        var changeSubsLength = changeSubs.Length;
                        for (int n = 0; n < changeSubsLength; n++) {
                            var writeTask = FilterUtils.FilterChanges(subClient, task, changeSubs[n], jsonEvaluator);
                            if (writeTask == null) {
                                continue;
                            }
                            AddTask(writeTask, ref eventTasks);
                            tasksAdded = true;
                        }
                        break;
                    }
                    case TaskType.message:
                    case TaskType.command:
                        var messageTask = (SyncMessageTask)task;
                        if (!IsEventTarget(subClient, messageTask)) {
                            continue;
                        }
                        if (!FilterMessage(messageTask.name)) {
                            continue;
                        }
                        // don't leak userId's & clientId's to subscribed clients
                        messageTask.users   = null;
                        messageTask.clients = null;
                        messageTask.groups  = null;
                        AddTask(messageTask, ref eventTasks);
                        tasksAdded = true;
                    break;
                }
            }
            return tasksAdded;
        }
        
        /// <summary>Add the <paramref name="task"/> to the <paramref name="eventTasks"/></summary>
        private static void AddTask(SyncRequestTask task, ref List<SyncRequestTask> eventTasks) {
            if (eventTasks == null) {
                eventTasks = new List<SyncRequestTask>();
            }
            eventTasks.Add(task);
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
        
        internal bool IsEqual(DatabaseSubs other) {
            return  messageSubs.        SetEquals(other.messageSubs) &&
                    messagePrefixSubs.  SetEquals(other.messagePrefixSubs);
        }
        
        internal int HashCode() {
            int hashCode = 0;
            foreach (var item in messageSubs) {
                hashCode ^= item.GetHashCode(); 
            }
            foreach (var item in messagePrefixSubs) {
                hashCode ^= item.GetHashCode(); 
            }
            foreach (var item in changeSubs) {
                hashCode ^= item.HashCode();
            }
            return hashCode;
        }
    }
    
    internal sealed class DatabaseSubsComparer : IEqualityComparer<DatabaseSubs>
    {
        public bool Equals(DatabaseSubs x, DatabaseSubs y) {
            // ReSharper disable once PossibleNullReferenceException
            return x.IsEqual(y);
        }

        public int GetHashCode(DatabaseSubs value) {
            return value.HashCode();
        }
    }
}