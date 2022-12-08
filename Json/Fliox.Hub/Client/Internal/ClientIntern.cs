// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Hub.Client.Internal.Map;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Utils;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal struct ClientIntern
    {
        // --- readonly
        internal readonly   FlioxHub                    hub;
        internal readonly   TypeStore                   typeStore;
        internal readonly   Pool                        pool;
        internal readonly   SharedEnv                   sharedEnv;
        internal readonly   IHubLogger                  hubLogger;
        internal            string                      database => databaseSmall.value;
        internal readonly   SmallString                 databaseSmall;
        /// <summary>is null if <see cref="FlioxHub.SupportPushEvents"/> == false</summary> 
        internal readonly   EventReceiver               eventReceiver;
        
        // --- readonly / private - owned
        private             ObjectDiffer                            objectDiffer;  // create on demand
        private             JsonMergeWriter                         mergeWriter;    // create on demand
        private             EntityProcessor                         processor;      // create on demand
        internal readonly   EntitySet[]                             entitySets;
        private  readonly   Dictionary<SmallString, EntitySet>      setByName;
        
        [DebuggerBrowsable(Never)]
        internal            Dictionary<string, MessageSubscriber>   subscriptions;          // create on demand - only used for subscriptions
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             IReadOnlyCollection<MessageSubscriber>  Subscriptions => subscriptions?.Values;

        internal            List<MessageSubscriber>                 subscriptionsPrefix;    // create on demand - only used for subscriptions
        // lock (pendingSyncs) instead of using ConcurrentDictionary<,> to avoid heap allocations
        internal readonly   Dictionary<Task, SyncContext>           pendingSyncs;
        private             List<JsonKey>                           idsBuf;     //  create buffer on demand - only used for Upsert

        // --- mutable state
        internal            SyncStore                   syncStore;
        internal            EventProcessor              eventProcessor;         // never null
        private             SubscriptionProcessor       subscriptionProcessor;  // lazy creation. Needed only if dealing with subscriptions 
        internal            ChangeSubscriptionHandler   changeSubscriptionHandler;
        internal            SubscriptionEventHandler    subscriptionEventHandler;
        internal            bool                        disposed;
        internal            int                         lastEventSeq;
        internal            int                         syncCount;
        internal            Timer                       ackTimer;
        internal            bool                        ackTimerPending;
        internal            JsonKey                     userId;
        internal            JsonKey                     clientId;
        internal            string                      token;
        
        internal            InstanceBuffer<SyncStore>   syncStoreBuffer;
        internal            InstanceBuffer<SyncRequest> syncRequestBuffer;
        internal            InstanceBuffer<MemoryBuffer>memoryBufferPool;
        internal            InstanceBuffer<SyncResult>  syncResultBuffer;
        internal            InstanceBuffer<SyncContext> syncContextBuffer;


        // --- create expensive / infrequently used objects on demand. Used method to avoid creation by debugger
        internal EntityProcessor        EntityProcessor()       => processor             ?? (processor             = new EntityProcessor());
        internal ObjectDiffer           ObjectDiffer()          => objectDiffer          ?? (objectDiffer          = new ObjectDiffer(typeStore));
        internal JsonMergeWriter        JsonMergeWriter()       => mergeWriter           ?? (mergeWriter           = new JsonMergeWriter(typeStore));
        internal SubscriptionProcessor  SubscriptionProcessor() => subscriptionProcessor ?? (subscriptionProcessor = new SubscriptionProcessor());
        internal List<JsonKey>          IdsBuf()                => idsBuf                ?? (idsBuf                = new List<JsonKey>());

        public   override string        ToString()              => "";

        private static readonly Dictionary<Type, ClientTypeInfo>    ClientTypeCache = new Dictionary<Type, ClientTypeInfo>();
        private static readonly DirectEventProcessor                DefaultEventProcessor = new DirectEventProcessor();

       
        internal EntitySet  GetSetByName    (SmallString name)                    => setByName[name];
        internal bool       TryGetSetByName (SmallString name, out EntitySet set) => setByName.TryGetValue(name, out set);

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
            
            // --- readonly
            sharedEnv               = hub.sharedEnv;
            typeStore               = sharedEnv.TypeStore;
            this.pool               = sharedEnv.Pool;
            this.hubLogger          = sharedEnv.hubLogger;
            this.hub                = hub;
            var databaseName        = database ?? (hub is RemoteClientHub remoteHub ? remoteHub.DatabaseName.value : null);
            databaseSmall           = new SmallString(databaseName);
            this.eventReceiver      = eventReceiver;
            
            // --- readonly / private - owned
            objectDiffer            = null;
            mergeWriter             = null;
            processor               = null;
            entitySets              = new EntitySet[entityInfos.Length];
            setByName               = new Dictionary<SmallString, EntitySet>(entityInfos.Length, SmallString.Equality);
            subscriptions           = null; 
            subscriptionsPrefix     = null; 
            pendingSyncs            = new Dictionary<Task, SyncContext>();
            idsBuf                  = null;

            // --- mutable state
            syncStore                   = new SyncStore();
            eventProcessor              = DefaultEventProcessor;
            changeSubscriptionHandler   = null;
            subscriptionEventHandler    = null;
            subscriptionProcessor       = null;
            disposed                    = false;
            lastEventSeq                = 0;
            syncCount                   = 0;
            ackTimer                    = null;
            ackTimerPending             = false;
            userId                      = new JsonKey();
            clientId                    = new JsonKey();
            token                       = null;
            
            syncStoreBuffer             = default;
            syncRequestBuffer           = default;
            memoryBufferPool            = default;
            syncResultBuffer            = default;
            syncContextBuffer           = default;
            
            InitEntitySets (client, entityInfos);
        }
        
        internal void Dispose() {
            // readonly - owned
            ackTimer?.Dispose();
            idsBuf?.Clear();
            lock (pendingSyncs) { pendingSyncs.Clear(); }
            disposed = true;
            // messageReader.Dispose();
            subscriptionProcessor?.Dispose();
            subscriptionsPrefix?.Clear();
            subscriptions?.Clear();
            hub.RemoveEventReceiver(clientId);
            setByName.Clear();
            processor?.Dispose();
            objectDiffer?.Dispose();
            mergeWriter?.Dispose();
        }
        
        internal void Reset () {
            hub.RemoveEventReceiver(clientId);
            userId          = new JsonKey();
            clientId        = new JsonKey();
            token           = null;
            lastEventSeq    = 0;
            syncCount       = 0;
            subscriptionsPrefix?.Clear();   // todo should assert if having open subscriptions 
            subscriptions?.Clear();         // todo should assert if having open subscriptions
            syncStore       = new SyncStore();
        }
        
        private void InitEntitySets(FlioxClient client, EntityInfo[] entityInfos) {
            var clientTypeInfo  = GetClientTypeInfo (client.type, entityInfos);
            var error           = clientTypeInfo.error;
            if (error != null) {
                throw new InvalidTypeException(error);
            }
            var mappers = clientTypeInfo.entitySetMappers;
            for (int n = 0; n < entityInfos.Length; n++) {
                var entityInfo  = entityInfos[n];
                var name        = new SmallString(entityInfo.container);
                var setMapper   = mappers[n];
                var entitySet   = setMapper.CreateEntitySet(name.value);
                entitySet.Init(client);
                entitySets[n]   = entitySet;
                setByName[name] = entitySet;
                entityInfo.SetEntitySetMember(client, entitySet);
            }
        }
        
        private ClientTypeInfo GetClientTypeInfo (Type clientType, EntityInfo[] entityInfos) {
            var cache = ClientTypeCache;
            lock (cache) {
                if (cache.TryGetValue(clientType, out var result))
                    return result;
                var mappers = new IEntitySetMapper[entityInfos.Length];
                for (int n = 0; n < entityInfos.Length; n++) {
                    var entitySetType = entityInfos[n].entitySetType;
                    mappers[n] = (IEntitySetMapper)typeStore.GetTypeMapper(entitySetType);
                }
                var error       = ValidateMappers(mappers, entityInfos);
                var clientInfo  = new ClientTypeInfo (mappers, error);
                cache.Add(clientType, clientInfo);
                return clientInfo;
            }
        }
        
        // Validate [Relation(<container>)] fields / properties
        private static string ValidateMappers(IEntitySetMapper[] mappers, EntityInfo[] entityInfos) {
            var entityInfoMap = entityInfos.ToDictionary(entityInfo => entityInfo.container);
            foreach (var mapper in mappers) {
                var typeMapper      = (TypeMapper)mapper;
                var entityMapper    = typeMapper.GetElementMapper();
                var fields          = entityMapper.PropFields.fields;
                foreach (var field in fields) {
                    var relation = field.relation;
                    if (relation == null)
                        continue;
                    if (!entityInfoMap.TryGetValue(relation, out var entityInfo)) {
                        return $"[Relation('{relation}')] at {entityMapper.type.Name}.{field.name} not found";
                    }
                    var fieldMapper     = field.fieldType;
                    var relationMapper  = fieldMapper.GetElementMapper() ?? fieldMapper;
                    var relationType    = relationMapper.nullableUnderlyingType ?? relationMapper.type;
                    var setKeyType      = entityInfo.keyType;
                    if (setKeyType != relationType) {
                        return $"[Relation('{relation}')] at {entityMapper.type.Name}.{field.name} invalid type. Expect: {setKeyType.Name}";
                    }
                }
            }
            return null;
        }

        internal Dictionary<string, SyncSet> CreateSyncSets(Dictionary<string,SyncSet> syncSets) {
            var count = 0;
            syncSets?.Clear();
            foreach (var set in entitySets) {
                SyncSet syncSet = set.SyncSet;
                if (syncSet == null)
                    continue;
                count++;
            }
            if (count == 0) {
                return syncSets;
            }
            // create Dictionary<,> only if required
            syncSets = syncSets ?? new Dictionary<string, SyncSet>(count);
            foreach (var set in entitySets) {
                SyncSet syncSet = set.SyncSet;
                if (syncSet == null)
                    continue;
                syncSets.Add(set.name, syncSet);
            }
            return syncSets;
        }
        
        internal SubscribeMessageTask AddCallbackHandler(string name, MessageCallback handler) {
            var task = new SubscribeMessageTask(name, null);
            var subs = subscriptions;
            if (subs == null) {
                subs = subscriptions = new Dictionary<string, MessageSubscriber>();
            } 
            if (!subs.TryGetValue(name, out var subscriber)) {
                subscriber = new MessageSubscriber(name);
                subs.Add(name, subscriber);
            } else {
                task.state.Executed = true;
            }
            if (subscriber.isPrefix) {
                if (subscriptionsPrefix == null) subscriptionsPrefix = new List<MessageSubscriber>();
                subscriptionsPrefix.Add(subscriber);
            }
            subscriber.callbackHandlers.Add(handler);
            return task;
        }
        
        internal SubscribeMessageTask RemoveCallbackHandler (string name, object handler) {
            var prefix      = SubscribeMessage.GetPrefix(name);
            var subsPrefix  = subscriptionsPrefix;
            if (prefix != null && subsPrefix != null) {
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
            if (subs == null || !subs.TryGetValue(name, out var subscriber)) {
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
        
        private readonly struct ClientTypeInfo
        {
            internal  readonly  string              error;
            internal  readonly  IEntitySetMapper[]  entitySetMappers;
        
            internal ClientTypeInfo (IEntitySetMapper[] entitySetMappers, string error) {
                this.entitySetMappers   = entitySetMappers;
                this.error              = error;
            }
        }
    }
}
