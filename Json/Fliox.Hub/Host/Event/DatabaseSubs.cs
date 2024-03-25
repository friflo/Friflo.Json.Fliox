// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
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
        private  readonly   HashSet<ShortString>                messageSubs;
        private  readonly   HashSet<ShortString>                messagePrefixSubs;
        /// <summary> key: <see cref="SubscribeChanges.container"/> </summary>
        private  readonly   Dictionary<ShortString, ChangeSub>  changeSubsMap;
        /// <summary> 'immutable' array of <see cref="changeSubsMap"/> values use for performance </summary>
        internal            ChangeSub[]                         changeSubs;

        public   override   string  ToString()  => GetString();
        internal            int     SubCount    => changeSubs.Length + messageSubs.Count + messagePrefixSubs.Count;
        
        internal static readonly  DatabaseSubsComparer Equality = new DatabaseSubsComparer();
        
        internal DatabaseSubs() {
            messageSubs         = new HashSet<ShortString>(ShortString.Equality);
            messagePrefixSubs   = new HashSet<ShortString>(ShortString.Equality);
            changeSubsMap       = new Dictionary<ShortString, ChangeSub>(ShortString.Equality);
            changeSubs          = Array.Empty<ChangeSub>();
        }
        
        internal DatabaseSubs(DatabaseSubs other) {
            messageSubs         = new HashSet<ShortString>(other.messageSubs,       ShortString.Equality);
            messagePrefixSubs   = new HashSet<ShortString>(other.messagePrefixSubs, ShortString.Equality);
            changeSubsMap       = new Dictionary<ShortString, ChangeSub>(other.changeSubsMap, ShortString.Equality);
            changeSubs          = new ChangeSub [other.changeSubs.Length];
            other.changeSubs.CopyTo(changeSubs, 0);
        }
        
        private string GetString() {
            var sb = new StringBuilder();
            var first = true;
            if (messageSubs.Count + messagePrefixSubs.Count > 0) {
                sb.Append("messages: [");
                foreach (var message in messageSubs) {
                    if (!first) sb.Append(", ");
                    first = false;
                    sb.Append(message);
                }
                foreach (var prefix in messagePrefixSubs) {
                    if (!first) sb.Append(", ");
                    first = false;
                    sb.Append(prefix);
                    sb.Append('*');
                }
                sb.Append(']');
            }
            if (changeSubsMap.Count > 0) {
                first = true;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append("changes: [");
                foreach (var pair in changeSubsMap) {
                    if (!first) sb.Append(", ");
                    first = false;
                    pair.Value.AppendTo(sb);
                }
                sb.Append(']');
            }
            return sb.ToString();
        }

        private bool FilterMessage (in ShortString messageName) {
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
                msgSubs.Add(messageSub.AsString());
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
                    container   = sub.container.AsString(),
                    changes     = ClusterUtils.FlagsToList(sub.changes),
                    filter      = sub.filter
                };
                subs.Add(changeSubscription);
            }
            return subs;
        }
        
        internal void RemoveMessageSubscription(string name) {
            var prefix = SubscribeMessage.GetPrefix(name);
            if (prefix.IsNull()) {
                messageSubs.Remove(new ShortString(name));
            } else {
                messagePrefixSubs.Remove(prefix);
            }
        }

        internal void AddMessageSubscription(string name) {
            var prefix = SubscribeMessage.GetPrefix(name);
            if (prefix.IsNull()) {
                messageSubs.Add(new ShortString(name));
            } else {
                messagePrefixSubs.Add(prefix);
            }
        }

        private void UpdateChangeSubs() {
            var subs = new ChangeSub[changeSubsMap.Count];
            int index = 0;
            foreach (var pair in changeSubsMap) {
                subs[index++] = pair.Value;
            }
            changeSubs = subs;
        }
        
        internal void RemoveChangeSubscription(in ShortString container) {
            changeSubsMap.Remove(container);
            UpdateChangeSubs();
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
            var changeSub = new ChangeSub(subscribe.container, subscribe.changes, jsonFilter);
            changeSubsMap[subscribe.container] = changeSub;
            UpdateChangeSubs();
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
            return  messageSubs.        SetEquals(other.messageSubs)        &&
                    messagePrefixSubs.  SetEquals(other.messagePrefixSubs)  &&
                    ChangeSub.IsEqual(changeSubsMap, other.changeSubsMap);
        }
        
        internal int HashCode() {
            int hashCode = 0;
            foreach (var message in messageSubs) {
                hashCode ^= message.HashCode(); 
            }
            foreach (var prefix in messagePrefixSubs) {
                hashCode ^= prefix.HashCode(); 
            }
            foreach (var changeSub in changeSubs) {
                hashCode ^= changeSub.HashCode();
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