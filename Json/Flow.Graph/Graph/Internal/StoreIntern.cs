// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Utils;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal struct StoreIntern
    {
        internal readonly   string                                  clientId;
        internal readonly   TypeStore                               typeStore;
        private  readonly   TypeStore                               ownedTypeStore;
        internal readonly   TypeCache                               typeCache;
        internal readonly   ObjectMapper                            jsonMapper;

        internal readonly   ObjectPatcher                           objectPatcher;
        
        internal readonly   EntityDatabase                          database;
        internal readonly   Dictionary<Type,   EntitySet>           setByType;
        internal readonly   Dictionary<string, EntitySet>           setByName;
        internal readonly   Pools                                   contextPools;
        internal readonly   EventTarget                             eventTarget;
        internal readonly   Dictionary<string, MessageSubscriber>   subscriptions;
        internal readonly   List<MessageSubscriber>                 subscriptionsPrefix;
        internal readonly   ObjectReader                            messageReader;
        
        // --- non readonly
        internal            SyncStore                               sync;
        internal            LogTask                                 tracerLogTask;
        internal            SubscriptionHandler                     subscriptionHandler;
        internal            bool                                    disposed;
        internal            int                                     lastEventSeq;
        internal            int                                     syncCount;
        

        public   override   string                                  ToString() => clientId;


        internal StoreIntern(
            string                  clientId,
            TypeStore               typeStore,
            TypeStore               owned,
            EntityDatabase          database,
            ObjectMapper            jsonMapper,
            EventTarget             eventTarget,
            SyncStore               sync)
        {
            this.clientId               = clientId;
            this.typeStore              = typeStore;
            this.ownedTypeStore         = owned;
            this.database               = database;
            this.jsonMapper             = jsonMapper;
            this.typeCache              = jsonMapper.writer.TypeCache;
            this.eventTarget            = eventTarget;
            this.sync                   = sync;
            setByType                   = new Dictionary<Type, EntitySet>();
            setByName                   = new Dictionary<string, EntitySet>();
            objectPatcher               = new ObjectPatcher(jsonMapper);
            contextPools                = new Pools(Pools.SharedPools);
            subscriptions               = new Dictionary<string, MessageSubscriber>();
            subscriptionsPrefix         = new List<MessageSubscriber>();
            messageReader               = new ObjectReader(typeStore, new NoThrowHandler());
            tracerLogTask               = null;
            subscriptionHandler         = null;
            lastEventSeq                = 0;
            disposed                    = false;
            syncCount                   = 0;
        }
        
        internal void Dispose() {
            disposed = true;
            messageReader.Dispose();
            subscriptionsPrefix.Clear();
            subscriptions.Clear();
            contextPools.Dispose(); // dispose nothing - LocalPool's are used
            database.RemoveEventTarget(clientId);
            objectPatcher.Dispose();
            jsonMapper.Dispose();
            ownedTypeStore?.Dispose();
        }
        
        internal SubscribeMessageTask AddCallbackHandler(string name, MessageCallback handler) {
            var task = new SubscribeMessageTask(name, null);
            if (!subscriptions.TryGetValue(name, out var subscriber)) {
                subscriber = new MessageSubscriber(name);
                subscriptions.Add(name, subscriber);
                sync.subscribeMessage.Add(task);
            } else {
                task.state.Synced = true;
            }
            if (subscriber.isPrefix) {
                subscriptionsPrefix.Add(subscriber);
            }
            subscriber.callbackHandlers.Add(handler);
            return task;
        }
        
        internal SubscribeMessageTask RemoveCallbackHandler (string name, object handler) {
            var prefix = SubscribeMessage.GetPrefix(name);
            if (prefix != null) {
                if (handler == null) {
                    subscriptionsPrefix.RemoveAll((sub) => sub.name == prefix);
                } else {
                    foreach (var sub in subscriptionsPrefix.Where(sub => sub.name == prefix)) {
                        sub.callbackHandlers.RemoveAll(callback => callback.HasHandler(handler));
                    }
                }
            }
            var task = new SubscribeMessageTask(name, true);
            if (!subscriptions.TryGetValue(name, out var subscriber)) {
                task.state.Synced = true;
                return task;
            }
            if (handler != null) {
                subscriber.callbackHandlers.RemoveAll((h) => h.HasHandler(handler));
            } else {
                subscriber.callbackHandlers.Clear();
            }
            if (subscriber.callbackHandlers.Count == 0) {
                subscriptions.Remove(name);
                sync.subscribeMessage.Add(task);
            } else {
                task.state.Synced = true;
            }
            return task;
        }
    }
}
