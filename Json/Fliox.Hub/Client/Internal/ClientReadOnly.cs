// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client.Internal.Map;
using Friflo.Json.Fliox.Hub.Host;
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
        internal readonly   bool                            isRemoteHub;
        internal readonly   TypeStore                       typeStore;
        internal readonly   Pool                            pool;
        internal readonly   SharedEnv                       sharedEnv;
        internal readonly   IHubLogger                      hubLogger;
        internal readonly   string                          database;
        internal readonly   ShortString                     databaseShort;
        /// <summary>is null if <see cref="FlioxHub.SupportPushEvents"/> == false</summary> 
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
            out string      typeError)
        {
            entityInfos             = ClientEntityUtils.GetEntitySetInfos (client.type);
            sharedEnv               = hub.sharedEnv;
            typeStore               = sharedEnv.typeStore;
            this.pool               = sharedEnv.pool;
            this.hubLogger          = sharedEnv.hubLogger;
            this.hub                = hub;
            this.isRemoteHub        = hub.IsRemoteHub;
            this.database           = database ?? hub.database.name;
            databaseShort           = new ShortString(this.database);
            responseReaderPool      = hub.GetResponseReaderPool();
            pendingSyncs            = new Dictionary<Task, SyncContext>();
            var info                = InitEntitySets (client, entityInfos, typeStore);
            typeError               = info.error;
            messagePrefix           = info.messagePrefix;
        }
        
        private static ClientTypeInfo InitEntitySets(FlioxClient client, EntitySetInfo[] entityInfos, TypeStore typeStore) {
            var clientTypeInfo  = GetClientTypeInfo (client.type, entityInfos, typeStore);
            var error           = clientTypeInfo.error;
            if (error != null) {
                return clientTypeInfo;
            }
            var length  = entityInfos.Length;
            for (int n = 0; n < length; n++) {
                entityInfos[n].containerMember.SetContainerMember(client, n);
            }
            return clientTypeInfo;
        }
        
        private static ClientTypeInfo GetClientTypeInfo (Type clientType, EntitySetInfo[] entityInfos, TypeStore typeStore) {
            var cache = ClientTypeCache;
            lock (cache) {
                if (cache.TryGetValue(clientType, out var result))
                    return result;
                var mappers = new IEntitySetMapper[entityInfos.Length];
                string error = null;

                for (int n = 0; n < entityInfos.Length; n++) {
                    var info = entityInfos[n];
                    try {
                        mappers[n] = (IEntitySetMapper)typeStore.GetTypeMapper(info.entitySetType);
                    }
                    catch (InvalidTypeException e) {
                        error = $"{e.Message}. {UsedBy}{clientType.Name}.{info.container}";
                        break;
                    }
                }
                error             ??= ValidateMappers(clientType, mappers, entityInfos);
                var prefix          = HubMessagesUtils.GetMessagePrefix(clientType.CustomAttributes);
                var clientInfo      = new ClientTypeInfo (prefix, error);
                cache.Add(clientType, clientInfo);
                return clientInfo;
            }
        }
        
        // Validate [Relation(<container>)] fields / properties
        private static string ValidateMappers(Type clientType, IEntitySetMapper[] mappers, EntitySetInfo[] entityInfos) {
            var entityInfoMap = entityInfos.ToDictionary(entityInfo => entityInfo.memberName);
            for (int n = 0; n < mappers.Length; n++) {
                var typeMapper      = (TypeMapper)mappers[n];
                var info            = entityInfos[n];
                var entityMapper    = typeMapper.GetElementMapper();
                var fields          = entityMapper.PropFields.fields;
                foreach (var field in fields) {
                    var relation = field.relation;
                    if (relation == null)
                        continue;
                    if (!entityInfoMap.TryGetValue(relation, out var entityInfo)) {
                        return $"[Relation('{relation}')] at {entityMapper.type.Name}.{field.name} not found. {UsedBy}{clientType.Name}.{info.container}";
                    }
                    var fieldMapper     = field.fieldType;
                    var relationMapper  = fieldMapper.GetElementMapper() ?? fieldMapper;
                    var relationType    = relationMapper.nullableUnderlyingType ?? relationMapper.type;
                    var setKeyType      = entityInfo.keyType;
                    if (setKeyType != relationType) {
                        return $"[Relation('{relation}')] at {entityMapper.type.Name}.{field.name} invalid type. Expect: {setKeyType.Name}. {UsedBy}{clientType.Name}.{info.container}";
                    }
                }
            }
            return null;
        }
        
        private const string UsedBy = "Used by: ";
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
