// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Hub.Client.Internal.Map;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal struct ClientIntern
    {
        // --- readonly
        internal readonly   FlioxHub                    hub;
        internal readonly   TypeStore                   typeStore;
        internal readonly   Pool                        pool;
        internal readonly   SharedCache                 sharedCache;
        internal readonly   IHubLogger                  hubLogger;
        internal readonly   string                      database;
        internal readonly   EventReceiver               eventReceiver;
        
        // --- readonly / private - owned
        private             ObjectPatcher                           objectPatcher;  // create on demand
        private             EntityProcessor                         processor;      // create on demand
        internal readonly   Dictionary<Type,   EntitySet>           setByType;
        private  readonly   Dictionary<string, EntitySet>           setByName;
        
        [DebuggerBrowsable(Never)]
        internal readonly   Dictionary<string, MessageSubscriber>   subscriptions;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             IReadOnlyCollection<MessageSubscriber>  Subscriptions => subscriptions.Values;

        internal readonly   List<MessageSubscriber>                 subscriptionsPrefix;
        internal readonly   ConcurrentDictionary<Task, SyncContext> pendingSyncs;
        internal readonly   List<JsonKey>                           idsBuf;

        // --- mutable state
        internal            SyncStore                   syncStore;
        internal            IEventProcessor             eventProcessor;         // never null
        private             SubscriptionProcessor       subscriptionProcessor;  // lazy creation. Needed only if dealing with subscriptions 
        internal            ChangeSubscriptionHandler   changeSubscriptionHandler;
        internal            SubscriptionEventHandler    subscriptionEventHandler;
        internal            bool                        disposed;
        internal            int                         lastEventSeq;
        internal            int                         syncCount;
        internal            JsonKey                     userId;
        internal            JsonKey                     clientId;
        internal            string                      token;

        // --- create expensive / infrequently used objects on demand. Used method to avoid creation by debugger
        internal EntityProcessor        EntityProcessor()       => processor             ?? (processor             = new EntityProcessor());
        internal ObjectPatcher          ObjectPatcher()         => objectPatcher         ?? (objectPatcher         = new ObjectPatcher());
        internal SubscriptionProcessor  SubscriptionProcessor() => subscriptionProcessor ?? (subscriptionProcessor = new SubscriptionProcessor());

        public   override string        ToString()              => "";

        private static readonly Dictionary<Type, IEntitySetMapper[]> MapperCache = new Dictionary<Type, IEntitySetMapper[]>();
        private static readonly DirectEventProcessor                 DefaultEventProcessor = new DirectEventProcessor();

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
        
        internal void SetSubscriptionProcessor(SubscriptionProcessor processor) {
            subscriptionProcessor?.Dispose();
            subscriptionProcessor = processor;
        }

        internal ClientIntern(
            FlioxClient     client,
            FlioxHub        hub,
            string          database,
            EventReceiver   eventReceiver)
        {
            var entityInfos         = ClientEntityUtils.GetEntityInfos (client.type);
            var sharedEnv           = hub.sharedEnv;
            
            // --- readonly
            typeStore               = sharedEnv.TypeStore;
            this.pool               = sharedEnv.Pool;
            this.sharedCache        = sharedEnv.sharedCache;
            this.hubLogger          = sharedEnv.hubLogger;
            this.hub                = hub;
            this.database           = database;
            this.eventReceiver      = eventReceiver;
            
            // --- readonly / private - owned
            objectPatcher           = null;
            processor               = null;
            setByType               = new Dictionary<Type,   EntitySet>(entityInfos.Length);
            setByName               = new Dictionary<string, EntitySet>(entityInfos.Length);
            subscriptions           = new Dictionary<string, MessageSubscriber>();  // could create lazy - needed only if dealing with subscriptions
            subscriptionsPrefix     = new List<MessageSubscriber>();                // could create lazy - needed only if dealing with subscriptions 
            pendingSyncs            = new ConcurrentDictionary<Task, SyncContext>();
            idsBuf                  = new List<JsonKey>();

            // --- mutable state
            syncStore                   = new SyncStore();
            eventProcessor              = DefaultEventProcessor;
            changeSubscriptionHandler   = null;
            subscriptionEventHandler    = null;
            subscriptionProcessor       = null;
            disposed                    = false;
            lastEventSeq                = 0;
            syncCount                   = 0;
            userId                      = new JsonKey();
            clientId                    = new JsonKey();
            token                       = null;
            
            InitEntitySets (client, entityInfos);
        }
        
        internal void Dispose() {
            // readonly - owned
            idsBuf.Clear();
            pendingSyncs.Clear();
            disposed = true;
            // messageReader.Dispose();
            subscriptionProcessor?.Dispose();
            subscriptionsPrefix.Clear();
            subscriptions.Clear();
            hub.RemoveEventReceiver(clientId);
            setByName.Clear();
            setByType.Clear();
            processor?.Dispose();
            objectPatcher?.Dispose();
        }
        
        internal void Reset () {
            hub.RemoveEventReceiver(clientId);
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
            var mappers = GetEntitySetMappers (client.type, entityInfos);
            for (int n = 0; n < entityInfos.Length; n++) {
                var entityInfo  = entityInfos[n];
                var name        = entityInfo.container;
                var setMapper   = mappers[n];
                var entitySet   = setMapper.CreateEntitySet(name);
                entitySet.Init(client);
                setByType[entityInfo.entityType]    = entitySet;
                setByName[name]                     = entitySet;
                entityInfo.SetEntitySetMember(client, entitySet);
            }
            ValidateMappers(mappers);
        }
        
        // Validate [Relation(<container>)] fields / properties
        // todo this validation can be done once per FlioxClient type
        private void ValidateMappers(IEntitySetMapper[] mappers) {
            foreach (var mapper in mappers) {
                var typeMapper      = (TypeMapper)mapper;
                var entityMapper    = typeMapper.GetElementMapper();
                var fields          = entityMapper.propFields.fields;
                foreach (var field in fields) {
                    var relation = field.relation;
                    if (relation == null)
                        continue;
                    if (!TryGetSetByName(relation, out var entitySet)) {
                        throw new InvalidTypeException($"[Relation('{relation}')] at {entityMapper.type.Name}.{field.name} not found");
                    }
                    var fieldMapper     = field.fieldType;
                    var relationMapper  = fieldMapper.GetElementMapper() ?? fieldMapper;
                    var relationType    = relationMapper.nullableUnderlyingType ?? relationMapper.type;
                    var setKeyType      = entitySet.KeyType;
                    if (setKeyType != relationType) {
                        throw new InvalidTypeException($"[Relation('{relation}')] at {entityMapper.type.Name}.{field.name} invalid type. Expect: {setKeyType.Name}");
                    }
                }
            }
        }
        
        private IEntitySetMapper[] GetEntitySetMappers (Type clientType, EntityInfo[] entityInfos) {
            if (MapperCache.TryGetValue(clientType, out var result))
                return result;
            var mappers = new IEntitySetMapper[entityInfos.Length];
            for (int n = 0; n < entityInfos.Length; n++) {
                var entitySetType = entityInfos[n].entitySetType;
                mappers[n] = (IEntitySetMapper)typeStore.GetTypeMapper(entitySetType);
            }
            MapperCache.Add(clientType, mappers);
            return mappers;
        }
        
        private static readonly IDictionary<string, SyncSet> EmptySynSet = new EmptyDictionary<string, SyncSet>();

        internal IDictionary<string, SyncSet> CreateSyncSets() {
            var count = 0;
            foreach (var pair in setByName) {
                SyncSet syncSet = pair.Value.SyncSet;
                if (syncSet == null)
                    continue;
                count++;
            }
            if (count == 0) {
                return EmptySynSet;
            }
            // create Dictionary<,> only if required
            var syncSets = new Dictionary<string, SyncSet>(count);
            foreach (var pair in setByName) {
                SyncSet syncSet = pair.Value.SyncSet;
                if (syncSet == null)
                    continue;
                string container = pair.Key;
                syncSets.Add(container, syncSet);
            }
            return syncSets;
        }
        
        internal SubscribeMessageTask AddCallbackHandler(string name, MessageCallback handler) {
            var task = new SubscribeMessageTask(name, null);
            var subs = subscriptions; 
            if (!subs.TryGetValue(name, out var subscriber)) {
                subscriber = new MessageSubscriber(name);
                subs.Add(name, subscriber);
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
                var subsPrefix = subscriptionsPrefix;
                if (handler == null) {
                    subsPrefix.RemoveAll((sub) => sub.name == prefix);
                } else {
                    foreach (var sub in subsPrefix.Where(sub => sub.name == prefix)) {
                        sub.callbackHandlers.RemoveAll(callback => callback.HasHandler(handler));
                    }
                }
            }
            var task = new SubscribeMessageTask(name, true);
            var subs = subscriptions;
            if (!subs.TryGetValue(name, out var subscriber)) {
                task.state.Executed = true;
                return task;
            }
            if (handler != null) {
                subscriber.callbackHandlers.RemoveAll((h) => h.HasHandler(handler));
            } else {
                subscriber.callbackHandlers.Clear();
            }
            if (subscriber.callbackHandlers.Count == 0) {
                subs.Remove(name);
            } else {
                task.state.Executed = true;
            }
            return task;
        }
    }
}
