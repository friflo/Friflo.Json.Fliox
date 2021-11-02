// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client.Internal.Map;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal struct ClientIntern
    {
        // readonly
        internal readonly   FlioxClient                                 baseClient;
        internal readonly   TypeStore                                   typeStore;
        internal readonly   Pools                                       pools;
        internal readonly   FlioxHub                                    hub;
        internal readonly   EntityDatabase                              database;
        internal readonly   EventTarget                                 eventTarget;
        internal readonly   ITracerContext                              tracerContext;
        
        // readonly - owned
        private  readonly   SubscriptionProcessor                       defaultProcessor;
        private             ObjectPatcher                               objectPatcher;  // create on demand
        private             EntityProcessor                             processor;      // create on demand
        internal readonly   Dictionary<Type,   EntitySet>               setByType;
        private  readonly   Dictionary<string, EntitySet>               setByName;
        internal readonly   Dictionary<string, MessageSubscriber>       subscriptions;
        internal readonly   List<MessageSubscriber>                     subscriptionsPrefix;
        internal readonly   ConcurrentDictionary<Task, MessageContext>  pendingSyncs;
        internal readonly   List<JsonKey>                               idsBuf;

        // --- non readonly
        internal            SyncStore                               syncStore;
        internal            LogTask                                 tracerLogTask;
        internal            SubscriptionProcessor                   subscriptionProcessor;
        internal            SubscriptionHandler                     subscriptionHandler;                
        internal            bool                                    disposed;
        internal            int                                     lastEventSeq;
        internal            int                                     syncCount;
        internal            JsonKey                                 userId;
        internal            JsonKey                                 clientId;
        internal            string                                  token;


        internal EntityProcessor    EntityProcessor()   => processor     ?? (processor      = new EntityProcessor());
        internal ObjectPatcher      ObjectPatcher()     => objectPatcher ?? (objectPatcher  = new ObjectPatcher());
        
        internal EntitySet GetSetByType(Type type) {
            return setByType[type];
        }
        
        internal bool TryGetSetByType(Type type, out EntitySet set) {
            return setByType.TryGetValue(type, out set);
        }
        
        internal EntitySet GetSetByName (string name) {
            return setByName[name];
        }
        
        internal bool TryGetSetByName (string name, out EntitySet set) {
            return setByName.TryGetValue(name, out set);
        }

        internal ClientIntern(
            FlioxClient             thisClient,
            FlioxClient             baseClient,
            Pools                   pools,
            FlioxHub                hub,
            EntityDatabase          database,
            ITracerContext          tracerContext,
            EventTarget             eventTarget)
        {
            var entityInfos             = ClientEntityUtils.GetEntityInfos (thisClient.GetType());
            // readonly
            this.baseClient             = baseClient;
            typeStore                   = pools.TypeStore;
            this.pools                  = pools;
            this.hub                    = hub;
            this.database               = database;
            this.eventTarget            = eventTarget;
            this.tracerContext          = tracerContext;
            
            // readonly - owned
            objectPatcher               = null;
            processor                   = null;
            defaultProcessor            = new SubscriptionProcessor(thisClient);
            setByType                   = new Dictionary<Type,   EntitySet>(entityInfos.Length);
            setByName                   = new Dictionary<string, EntitySet>(entityInfos.Length);
            subscriptions               = new Dictionary<string, MessageSubscriber>();
            subscriptionsPrefix         = new List<MessageSubscriber>();
            pendingSyncs                = new ConcurrentDictionary<Task, MessageContext>();
            idsBuf                      = new List<JsonKey>();

            // --- non readonly
            syncStore                   = new SyncStore();
            tracerLogTask               = null;
            subscriptionProcessor       = defaultProcessor;
            subscriptionHandler         = null;
            disposed                    = false;
            lastEventSeq                = 0;
            syncCount                   = 0;
            userId                      = new JsonKey();
            clientId                    = new JsonKey();
            token                       = null;
            InitEntitySets (thisClient, entityInfos);
        }
        
        internal void Dispose() {
            // readonly - owned
            idsBuf.Clear();
            pendingSyncs.Clear();
            disposed = true;
            // messageReader.Dispose();
            subscriptionsPrefix.Clear();
            subscriptions.Clear();
            hub.RemoveEventTarget(clientId);
            setByName.Clear();
            setByType.Clear();
            defaultProcessor.Dispose();
            processor?.Dispose();
            objectPatcher?.Dispose();
        }
        
        internal void Reset () {
            hub.RemoveEventTarget(clientId);
            userId          = new JsonKey();
            clientId        = new JsonKey();
            token           = null;
            lastEventSeq    = 0;
            syncCount       = 0;
            subscriptionsPrefix.Clear();    // todo should assert if having open subscriptions 
            subscriptions.Clear();          // todo should assert if having open subscriptions
            syncStore       = new SyncStore();
        }
        
        private void InitEntitySets(FlioxClient client, EntityInfo[] entityInfos) {
            foreach (var entityInfo in entityInfos) {
                var name        = entityInfo.container;
                var setMapper   = (IEntitySetMapper)typeStore.GetTypeMapper(entityInfo.entitySetType);
                var entitySet   = setMapper.CreateEntitySet(name);
                entitySet.Init(client);
                setByType[entityInfo.entityType]    = entitySet;
                setByName[name]                     = entitySet;
                entityInfo.SetEntitySetMember(client, entitySet);
            }
        }
        
        internal Dictionary<string, SyncSet> CreateSyncSets() {
            var syncSets = new Dictionary<string, SyncSet>(setByName.Count);
            foreach (var pair in setByName) {
                string      container   = pair.Key;
                EntitySet   set         = pair.Value;
                SyncSet     syncSet     = set.SyncSet;
                if (syncSet != null) {
                    syncSets.Add(container, set.SyncSet);
                }
            }
            return syncSets;
        }
        
        internal SubscribeMessageTask AddCallbackHandler(string name, MessageCallback handler) {
            var task = new SubscribeMessageTask(name, null);
            if (!subscriptions.TryGetValue(name, out var subscriber)) {
                subscriber = new MessageSubscriber(name);
                subscriptions.Add(name, subscriber);
                syncStore.SubscribeMessage().Add(task);
            } else {
                task.state.Executed = true;
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
                task.state.Executed = true;
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
                task.state.Executed = true;
            }
            return task;
        }
    }
}
