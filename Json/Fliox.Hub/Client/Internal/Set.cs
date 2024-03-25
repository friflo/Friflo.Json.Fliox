// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Mapper;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    // --------------------------------------- EntitySet ---------------------------------------
    internal abstract partial class Set
    {
        [Browse(Never)] internal readonly   FlioxClient     client;
        [Browse(Never)] internal readonly   string          name;
        [Browse(Never)] internal readonly   int             index;
        [Browse(Never)] internal readonly   ShortString     nameShort;
        [Browse(Never)] internal readonly   string          keyName;
        [Browse(Never)] internal readonly   bool            isIntKey;
        [Browse(Never)] internal            ChangeCallback  changeCallback;
        
        [Browse(Never)] internal            InstanceBuffer<UpsertEntities>  upsertEntitiesBuffer;
        [Browse(Never)] internal            InstanceBuffer<ReadEntities>    readEntitiesBuffer;


        internal  abstract  SetInfo     SetInfo     { get; }
        internal  abstract  Type        KeyType     { get; }
        internal  abstract  Type        EntityType  { get; }
        internal  abstract  bool        WritePretty { get; set; }
        internal  abstract  bool        WriteNull   { get; set; }
        
        internal  abstract  void                Reset                   ();
        internal  abstract  void                DetectSetPatchesInternal(DetectAllPatches task, ObjectMapper mapper);
        internal  abstract  SyncTask            SubscribeChangesInternal(Change change);
        internal  abstract  SubscribeChanges    GetSubscription();
        internal  abstract  void                GetRawEntities(List<object> result);
        internal  abstract  Entity[]            AddReferencedEntities (ReferencesResult referenceResult, ObjectReader reader);
        
        internal readonly struct SetInit
        {
            internal readonly   FlioxClient     client;
            internal readonly   string          name;
            internal readonly   int             index;
            internal readonly   string          keyName;
            internal readonly   bool            isIntKey;
            
            internal SetInit (string name, int index, string keyName, bool isIntKey, FlioxClient client) {
                this.name       = name;
                this.index      = index;
                this.client     = client;
                this.keyName    = keyName;
                this.isIntKey   = isIntKey;
            }
        }
        
        protected Set(in SetInit init) {
            name        = init.name;
            index       = init.index;
            client      = init.client;
            keyName     = init.keyName;
            isIntKey    = init.isIntKey;
            nameShort   = new ShortString(name);
        }
        
        internal static void SetTaskInfo(ref SetInfo info, SyncTask[] tasks) {
            foreach (var syncTask in tasks) {
                switch (syncTask.TaskType) {
                    case TaskType.read:             info.read++;                break;
                    case TaskType.query:            info.query++;               break;
                    case TaskType.aggregate:        info.aggregate++;           break;
                    case TaskType.create:           info.create++;              break;
                    case TaskType.upsert:           info.upsert++;              break;
                    case TaskType.merge:            info.merge++;               break;
                    case TaskType.delete:           info.delete++;              break;
                    case TaskType.closeCursors:     info.closeCursors++;        break;
                    case TaskType.subscribeChanges: info.subscribeChanges++;    break;
                    case TaskType.reserveKeys:      info.reserveKeys++;         break;
                }
            }
            info.tasks =
                info.read               +
                info.query              +
                info.aggregate          +
                info.closeCursors       +
                info.create             +
                info.upsert             +
                info.merge              +
                info.delete             +
                info.subscribeChanges   +
                info.reserveKeys;
        }
        
        /// <summary>Counterpart of <see cref="EntitiesToJson"/></summary>
        //  SYNC_READ : JSON -> entities
        internal EntityValue[] JsonToEntities(
            ListOne<JsonValue>  set,
            List<JsonKey>       notFound,
            List<EntityError>   errors)
        {
            var processor   = client._intern.EntityProcessor();
            var values = new EntityValue[set.Count + (notFound?.Count ?? 0) + (errors?.Count ?? 0)];
            var n = 0;
            foreach (var value in set.GetReadOnlySpan()) {
                if (processor.GetEntityKey(value, keyName, out var key, out var error)) {
                    values[n++] = new EntityValue(key, value);
                } else {
                    throw new InvalidOperationException($"missing key int result: {error}");
                }
            }
            if (notFound != null) {
                foreach (var key in notFound) {
                    values[n++] = new EntityValue(key);
                }
            }
            if (errors != null) {
                foreach (var error in errors) {
                    error.container = nameShort; // container name is not serialized as it is redundant data.
                    values[n++]     = new EntityValue(error.id, error);
                }
            }
            return values;
        }
        
        /// <summary>Counterpart of <see cref="JsonToEntities"/></summary>
        //  SYNC_READ : entities -> JSON
        internal static void EntitiesToJson(
            EntityValue[]           values,
            out ListOne<JsonValue>  set,
            out List<JsonKey>       notFound,
            out List<EntityError>   errors)
        {
            set         = new ListOne<JsonValue>(values.Length);
            errors      = null;
            notFound    = null;
            foreach (var value in values) {
                var error = value.Error;
                if (error != null) {
                    errors ??= new List<EntityError>();
                    errors.Add(error);
                    continue;
                }
                if (!value.Json.IsNull()) {
                    set.Add(value.Json);
                } else {
                    notFound ??= new List<JsonKey>();
                    notFound.Add(value.key);
                }
            }
        }
    }
    
    internal readonly struct Entity
    {
        internal readonly object        value;
        internal readonly EntityError   error;
        
        internal Entity(object value, EntityError error) {
            this.value  = value;
            this.error  = error;
        }
    }
    
    // --------------------------------------- InternSet<TKey, T> ---------------------------------------
    internal sealed partial class Set<TKey, T> : Set<T>  where T : class
    {
        // Keep all utility related fields of EntitySet in SetIntern (intern) to enhance debugging overview.
        // Reason:  EntitySet<,> is used as field or property by an application which is mainly interested
        //          in following properties while debugging:
        //          Peers, Tasks
                        internal            SetIntern<TKey, T>          intern;         // Use intern struct as first field 
                        
        /// <summary> key: <see cref="Peer{TKey,T}.entity"/>.id </summary>
        [Browse(Never)] private readonly Dictionary<TKey,Peer<TKey, T>> peerMap;        //  Note: must be private by all means
        [Browse(Never)] private             bool                        TrackEntities   =>  peerMap != null;
        
        /// <summary> enable access to entities in debugger. Not used internally. </summary>
        // Note: using Dictionary.Values is okay. The ValueCollection is instantiated only once for a Dictionary instance
        // ReSharper disable once UnusedMember.Local
                        private    IReadOnlyCollection<Peer<TKey, T>>   Peers           => peerMap?.Values;
        
        [Browse(Never)] internal            LocalEntities<TKey,T>       Local           => local   ??= new LocalEntities<TKey, T>(this);
        [Browse(Never)] private             LocalEntities<TKey,T>       local;
        /// <summary> Note! Must be called only from <see cref="LocalEntities{TKey,T}"/> to preserve maintainability </summary>
                        internal          Dictionary<TKey,Peer<TKey,T>> GetPeers()      => peerMap;
                        public   override   string                      ToString()      => SetInfo.ToString();

        [Browse(Never)] internal override   SetInfo                     SetInfo         => GetSetInfo();
        [Browse(Never)] internal override   Type                        KeyType         => typeof(TKey);
        [Browse(Never)] internal override   Type                        EntityType      => typeof(T);
        
        /// <summary> If true the serialization of entities to JSON is prettified </summary>
        [Browse(Never)] internal override   bool                        WritePretty { get => intern.writePretty;   set => intern.writePretty = value; }
        /// <summary> If true the serialization of entities to JSON write null fields. Otherwise null fields are omitted </summary>
        [Browse(Never)] internal override   bool                        WriteNull   { get => intern.writeNull;     set => intern.writeNull   = value; }
        
        internal    InstanceBuffer<DeleteTask<TKey,T>>                  deleteBuffer;
        internal    InstanceBuffer<ReadTask<TKey, T>>                   readBuffer;

        /*
        /// <summary> using a static class prevents noise in form of 'Static members' for class instances in Debugger </summary>
        // private static class Static  { } */
        private static  readonly       EntityKeyT<TKey, T>         EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();
        private static  readonly       KeyConverter<TKey>          KeyConvert      = KeyConverter.GetConverter<TKey>();
        

        internal Set(string name, int index, FlioxClient client)
            : base (new SetInit(name, index, EntityKeyTMap.GetKeyName(), EntityKeyTMap.IsIntKey(), client))
        {
            // ValidateKeyType(typeof(TKey)); // only required if constructor is public
            // intern    = new SetIntern<TKey, T>(this);
            peerMap = client.TrackEntities ? CreateDictionary<TKey,Peer<TKey, T>>() : null;
        }
    }
}