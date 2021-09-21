// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.DB.Graph.Internal
{
    internal struct StoreIntern
    {
        // readonly
        internal readonly   string                                      clientId;
        internal readonly   TypeStore                                   typeStore;
        internal readonly   TypeCache                                   typeCache;
        internal readonly   EntityDatabase                              database;
        internal readonly   EventTarget                                 eventTarget;
        // readonly - owned
        internal readonly   ObjectMapper                                jsonMapper;
        internal readonly   ObjectPatcher                               objectPatcher;
        internal readonly   EntityProcessor                             processor;
        internal readonly   Dictionary<Type,   EntitySet>               setByType;
        internal readonly   Dictionary<string, EntitySet>               setByName;
        internal readonly   Dictionary<string, MessageSubscriber>       subscriptions;
        internal readonly   List<MessageSubscriber>                     subscriptionsPrefix;
        internal readonly   ObjectReader                                messageReader;
        internal readonly   ConcurrentDictionary<Task, MessageContext>  pendingSyncs;
        internal readonly   List<JsonKey>                               idsBuf;
        internal readonly   Pools                                       pools;

        // --- non readonly
        internal            SyncStore                               syncStore;
        internal            LogTask                                 tracerLogTask;
        internal            SubscriptionProcessor                   subscriptionProcessor;
        internal            SubscriptionHandler                     subscriptionHandler;                
        internal            bool                                    disposed;
        internal            int                                     lastEventSeq;
        internal            int                                     syncCount;
        internal            string                                  token;

        public   override   string                                  ToString() => clientId;


        internal StoreIntern(
            string                  clientId,
            TypeStore               typeStore,
            EntityDatabase          database,
            ITracerContext          tracerContext,
            EventTarget             eventTarget,
            SubscriptionProcessor   subscriptionProcessor)
        {
            // throw no exceptions on errors. Errors are handled by checking <see cref="ObjectReader.Success"/> 
            var mapper                  = new ObjectMapper(typeStore, new NoThrowHandler());
            mapper.TracerContext        = tracerContext;
            // readonly
            this.clientId               = clientId;
            this.typeStore              = typeStore;
            this.typeCache              = mapper.writer.TypeCache;
            this.database               = database;
            this.eventTarget            = eventTarget;
            // readonly - owned
            jsonMapper                  = mapper;
            objectPatcher               = new ObjectPatcher(jsonMapper);
            processor                   = new EntityProcessor();
            setByType                   = new Dictionary<Type, EntitySet>();
            setByName                   = new Dictionary<string, EntitySet>();
            subscriptions               = new Dictionary<string, MessageSubscriber>();
            subscriptionsPrefix         = new List<MessageSubscriber>();
            messageReader               = mapper.reader; // new ObjectReader(typeStore, new NoThrowHandler());
            pendingSyncs                = new ConcurrentDictionary<Task, MessageContext>();
            idsBuf                      = new List<JsonKey>();
            pools                       = new Pools(Pools.SharedPools);
            
            // --- non readonly
            syncStore                   = null;
            tracerLogTask               = null;
            this.subscriptionProcessor  = subscriptionProcessor;
            subscriptionHandler         = null;
            disposed                    = false;
            lastEventSeq                = 0;
            syncCount                   = 0;
            token                       = null;
        }
        
        internal void Dispose() {
            // readonly - owned
            pools.Dispose();
            idsBuf.Clear();
            pendingSyncs.Clear();
            disposed = true;
            // messageReader.Dispose();
            subscriptionsPrefix.Clear();
            subscriptions.Clear();
            database.RemoveEventTarget(clientId);
            setByName.Clear();
            setByType.Clear();
            processor.Dispose();
            objectPatcher.Dispose();
            // readonly
            jsonMapper.Dispose();
        }
        
        internal SubscribeMessageTask AddCallbackHandler(string name, MessageCallback handler) {
            var task = new SubscribeMessageTask(name, null);
            if (!subscriptions.TryGetValue(name, out var subscriber)) {
                subscriber = new MessageSubscriber(name);
                subscriptions.Add(name, subscriber);
                syncStore.SubscribeMessage().Add(task);
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
                syncStore.SubscribeMessage().Add(task);
            } else {
                task.state.Synced = true;
            }
            return task;
        }
    }
}
