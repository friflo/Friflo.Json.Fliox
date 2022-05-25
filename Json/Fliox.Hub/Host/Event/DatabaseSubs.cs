// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal class DatabaseSubs
    {
        private  readonly   string                                  database;

        public   override   string                                  ToString() => database;

        /// key: <see cref="SubscribeChanges.container"/>
        internal readonly   Dictionary<string, SubscribeChanges>    changeSubscriptions         = new Dictionary<string, SubscribeChanges>();
        private  readonly   HashSet<string>                         messageSubscriptions        = new HashSet<string>();
        private  readonly   HashSet<string>                         messagePrefixSubscriptions  = new HashSet<string>();
        
        internal            int                                     SubscriptionCount => changeSubscriptions.Count + messageSubscriptions.Count + messagePrefixSubscriptions.Count; 

        
        internal DatabaseSubs (string database) {
            this.database   = database;
        }
        
        internal bool FilterMessage (string messageName) {
            if (messageSubscriptions.Contains(messageName))
                return true;
            foreach (var prefixSub in messagePrefixSubscriptions) {
                if (messageName.StartsWith(prefixSub)) {
                    return true;
                }
            }
            return false;
        }
        
        internal List<string> GetMessageSubscriptions (List<string> msgSubs) {
            foreach (var messageSub in messageSubscriptions) {
                if (msgSubs == null) msgSubs = new List<string>();
                msgSubs.Add(messageSub);
            }
            foreach (var messageSub in messagePrefixSubscriptions) {
                if (msgSubs == null) msgSubs = new List<string>();
                msgSubs.Add(messageSub + "*");
            }
            return msgSubs;
        }
        
        internal List<ChangeSubscription> GetChangeSubscriptions (List<ChangeSubscription> subs) {
            if (changeSubscriptions.Count == 0)
                return null;
            if (subs == null) subs = new List<ChangeSubscription>(changeSubscriptions.Count);
            subs.Clear();
            subs.Capacity = changeSubscriptions.Count;
            foreach (var pair in changeSubscriptions) {
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
                messageSubscriptions.Remove(name);
            } else {
                messagePrefixSubscriptions.Remove(prefix);
            }
        }

        internal void AddMessageSubscription(string name) {
            var prefix = SubscribeMessage.GetPrefix(name);
            if (prefix == null) {
                messageSubscriptions.Add(name);
            } else {
                messagePrefixSubscriptions.Add(prefix);
            }
        }

        internal void RemoveChangeSubscription(string container) {
            changeSubscriptions.Remove(container);
        }

        internal void AddChangeSubscription(SubscribeChanges subscribe) {
            changeSubscriptions[subscribe.container] = subscribe;
        }
    }
}