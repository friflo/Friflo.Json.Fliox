// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client.Internal.Map;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal readonly struct ClientReadOnly
    {
        // --- readonly
        internal readonly   FlioxHub                        hub;
        internal readonly   TypeStore                       typeStore;
        internal readonly   Pool                            pool;
        internal readonly   SharedEnv                       sharedEnv;
        internal readonly   IHubLogger                      hubLogger;
        internal readonly   string                          database;
        internal readonly   ShortString                     databaseShort;
        /// <summary>is null if <see cref="FlioxHub.SupportPushEvents"/> == false</summary> 
        internal readonly   EventReceiver                   eventReceiver;
        internal readonly   ObjectPool<ReaderPool>          responseReaderPool;
        internal readonly   string                          messagePrefix;
        internal readonly   EntitySetInfo[]                 entityInfos;
        // lock (pendingSyncs) instead of using ConcurrentDictionary<,> to avoid heap allocations
        internal readonly   Dictionary<Task, SyncContext>   pendingSyncs;
        
        private static readonly Dictionary<Type, ClientTypeInfo>    ClientTypeCache         = new Dictionary<Type, ClientTypeInfo>();

        
        internal ClientReadOnly(
            FlioxClient     client,
            FlioxHub        hub,
            string          database,
            EventReceiver   eventReceiver)
        {
            entityInfos             = ClientEntityUtils.GetEntitySetInfos (client.type);
            sharedEnv               = hub.sharedEnv;
            typeStore               = sharedEnv.typeStore;
            this.pool               = sharedEnv.pool;
            this.hubLogger          = sharedEnv.hubLogger;
            this.hub                = hub;
            this.database           = database ?? hub.database.name;
            databaseShort           = new ShortString(this.database);
            this.eventReceiver      = eventReceiver;
            responseReaderPool      = hub.GetResponseReaderPool();
            pendingSyncs            = new Dictionary<Task, SyncContext>();
            messagePrefix           = null;
            var info = InitEntitySets (client, entityInfos);
            messagePrefix           = info.messagePrefix;
        }
        
        private ClientTypeInfo InitEntitySets(FlioxClient client, EntitySetInfo[] entityInfos) {
            var clientTypeInfo  = GetClientTypeInfo (client.type, entityInfos);
            var error           = clientTypeInfo.error;
            if (error != null) {
                throw new InvalidTypeException(error);
            }
            var length  = entityInfos.Length;
            for (int n = 0; n < length; n++) {
                entityInfos[n].containerMember.SetContainerMember(client, n);
            }
            return clientTypeInfo;
        }
        
        private ClientTypeInfo GetClientTypeInfo (Type clientType, EntitySetInfo[] entityInfos) {
            var cache = ClientTypeCache;
            lock (cache) {
                if (cache.TryGetValue(clientType, out var result))
                    return result;
                var mappers = new IEntitySetMapper[entityInfos.Length];
                for (int n = 0; n < entityInfos.Length; n++) {
                    var entitySetType = entityInfos[n].entitySetType;
                    mappers[n] = (IEntitySetMapper)typeStore.GetTypeMapper(entitySetType);
                }
                var error           = ValidateMappers(mappers, entityInfos);
                var prefix          = HubMessagesUtils.GetMessagePrefix(clientType.CustomAttributes);
                var clientInfo      = new ClientTypeInfo (prefix, error);
                cache.Add(clientType, clientInfo);
                return clientInfo;
            }
        }
        
        // Validate [Relation(<container>)] fields / properties
        private static string ValidateMappers(IEntitySetMapper[] mappers, EntitySetInfo[] entityInfos) {
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
    }
    
    internal readonly struct ClientTypeInfo
    {
        internal  readonly  string              error;
        internal  readonly  string              messagePrefix;
        
        internal ClientTypeInfo (
            string              messagePrefix,
            string              error)
        {
            this.messagePrefix      = messagePrefix;
            this.error              = error;
        }
    }
}
