// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal class DatabaseSubs
    {
        
        internal readonly  string                                   database;

        /// key: <see cref="SubscribeChanges.container"/>
        internal readonly   Dictionary<string, SubscribeChanges>    changeSubscriptions         = new Dictionary<string, SubscribeChanges>();
        internal readonly   HashSet<string>                         messageSubscriptions        = new HashSet<string>();
        internal readonly   HashSet<string>                         messagePrefixSubscriptions  = new HashSet<string>();
        
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
    }
}