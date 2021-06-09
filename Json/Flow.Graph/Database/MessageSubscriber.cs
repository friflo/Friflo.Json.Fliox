// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
    public class MessageSubscriber {
        private  readonly   IMessageTarget                  messageTarget;
        internal readonly   SubscribeChanges                subscribe;
        internal readonly   ConcurrentQueue<ChangesMessage> queue = new ConcurrentQueue<ChangesMessage>();
        
        public MessageSubscriber (IMessageTarget messageTarget, SubscribeChanges subscribe) {
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
                    var success = await messageTarget.SendMessage(changeMessage, syncContext).ConfigureAwait(false);
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
}