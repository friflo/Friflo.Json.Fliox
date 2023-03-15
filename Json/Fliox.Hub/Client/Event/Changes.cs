// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Defines signature of the handler method passed to <see cref="FlioxClient.SubscribeAllChanges"/>
    /// </summary>
    /// <seealso cref="SubscriptionEventHandler"/>
    public delegate void ChangeSubscriptionHandler         (EventContext context);
    /// <summary>
    /// Defines signature of the handler method passed to <see cref="EntitySet{TKey,T}.SubscribeChanges"/>
    /// </summary>
    /// <seealso cref="SubscriptionEventHandler"/>
    public delegate void ChangeSubscriptionHandler<TKey, T>(Changes<TKey, T> changes, EventContext context) where T : class;
    
    /// <summary>
    /// Contain <b>raw</b> changes (mutations) made to a container subscribed with <see cref="EntitySet{TKey,T}.SubscribeChanges"/>.
    /// </summary>
    public abstract class Changes
    {
        /// <summary> total number of container changes </summary>
        public                                          int                 Count       => changeInfo.Count;
        /// <summary> number of changes per mutation type: creates, upserts, deletes and patches </summary>
        public                                          ChangeInfo          ChangeInfo  => changeInfo;
        /// <summary> name of the container the changes are referring to </summary>
        public    abstract                              string              Container       { get; }
        public    abstract                              ShortString         ContainerShort  { get; }
        /// <summary> raw JSON values of created container entities </summary>
        public                                          List<JsonEntity>    RawCreates  => rawCreates;
        /// <summary> raw JSON values of upserted container entities </summary>
        public                                          List<JsonEntity>    RawUpserts  => rawUpserts;
        
        [DebuggerBrowsable(Never)]  internal            bool                added;
        [DebuggerBrowsable(Never)]  internal            ChangeInfo          changeInfo;
        [DebuggerBrowsable(Never)]  internal  readonly  List<JsonEntity>    rawCreates  = new List<JsonEntity>();
        [DebuggerBrowsable(Never)]  internal  readonly  List<JsonEntity>    rawUpserts  = new List<JsonEntity>();

        internal  abstract  void        Clear       ();
        internal  abstract  void        AddDeletes  (List<JsonKey> ids);
        internal  abstract  void        AddPatches  (List<JsonEntity> patches, FlioxClient client);
        internal  abstract  void        ApplyChangesToInternal  (EntitySet entitySet);
    }
    
    /// <summary>
    /// Contain <b>strongly typed</b> changes (mutations) made to a container subscribed with <see cref="EntitySet{TKey,T}.SubscribeChanges"/>.
    /// </summary>
    /// <remarks>
    /// Following properties provide type-safe access to the different types of container changes 
    /// <list type="bullet">
    ///   <item> <see cref="Creates"/> - the created container entities</item>
    ///   <item> <see cref="Upserts"/> - the upserted container entities</item>
    ///   <item> <see cref="Deletes"/> - the keys of removed container entities</item>
    ///   <item> <see cref="Patches"/> - the patches applied to container entities</item>
    /// </list>
    /// Container <see cref="Changes{TKey,T}"/> are not automatically applied to an <see cref="EntitySet{TKey,T}"/>.
    /// To apply container changes to a <see cref="EntitySet{TKey,T}"/> call <see cref="ApplyChangesTo(EntitySet{TKey,T},Change)"/>.
    /// </remarks>
    public sealed class Changes<TKey, T> : Changes where T : class
    {
        /// <summary> return the entities created in a container </summary>
        public              List<Create<TKey,T>>    Creates         => GetCreates();
        /// <summary> return the entities upserted in a container </summary>
        public              List<Upsert<TKey,T>>    Upserts         => GetUpserts();
        /// <summary> return the keys of removed container entities </summary>
        public              List<Delete<TKey>>         Deletes { get; } = new List<Delete<TKey>>();
        /// <summary> return patches applied to container entities </summary>
        public              List<Patch<TKey>>       Patches { get; } = new List<Patch<TKey>>();
        
        private   readonly  List<ApplyInfo<TKey,T>> applyInfos  = new List<ApplyInfo<TKey,T>>();

        public    override  string                  ToString()      => FormatToString();       
        public    override  string                  Container       { get; }
        public    override  ShortString             ContainerShort  { get; }
        
        [DebuggerBrowsable(Never)] private          List<Create<TKey,T>>    creates;
        [DebuggerBrowsable(Never)] private          List<Upsert<TKey,T>>    upserts;
        [DebuggerBrowsable(Never)] private readonly ObjectMapper            objectMapper;
        [DebuggerBrowsable(Never)] private readonly string                  keyName;
        
        private static  readonly    EntityKeyT<TKey, T> EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();

        
        private static readonly KeyConverter<TKey>  KeyConvert = KeyConverter.GetConverter<TKey>();

        /// <summary> called via <see cref="SubscriptionProcessor.GetChanges"/> </summary>
        internal Changes(EntitySet<TKey, T> entitySet, ObjectMapper mapper) {
            keyName         = entitySet.GetKeyName();
            Container       = entitySet.name;
            ContainerShort  = entitySet.nameShort;
            objectMapper    = mapper;
        }
        
        /// <summary>
        /// add the keys of all <see cref="Creates"/>, <see cref="Upserts"/>, <see cref="Deletes"/> and <see cref="Patches"/>
        /// to the passed <paramref name="keys"/> collection. 
        /// </summary>
        public void GetKeys(ICollection<TKey> keys) {
            foreach (var create in Creates) { keys.Add(create.key); }
            foreach (var upsert in Upserts) { keys.Add(upsert.key); }
            foreach (var delete in Deletes) { keys.Add(delete.key); }    
            foreach (var patch  in Patches) { keys.Add(patch.key);  }
        }
        
        private string FormatToString() {
            var sb = new StringBuilder();
            sb.Append(Container);
            sb.Append(" - ");
            changeInfo.AppendTo(sb);
            return sb.ToString();
        }
        
        internal override void Clear() {
            added   = false;
            creates = null;
            upserts = null;
            Deletes.Clear();
            Patches.Clear();
            
            rawCreates.Clear();
            rawUpserts.Clear();
            //
            changeInfo.Clear();
        }
        
        private List<Create<TKey,T>> GetCreates() {
            if (creates != null)
                return creates;
            // create entities on demand
            var entities = rawCreates;
            creates = new List<Create<TKey,T>>(entities.Count); // list could be reused
            foreach (var create in entities) {
                var entity  = objectMapper.Read<T>(create.value);
                var key     = EntityKeyTMap.GetKey(entity);
                creates.Add(new Create<TKey, T>(key, entity));
            }
            return creates;
        }
        
        private List<Upsert<TKey,T>> GetUpserts() {
            if (upserts != null)
                return upserts;
            // create entities on demand
            var entities = rawUpserts;
            upserts = new List<Upsert<TKey,T>>(entities.Count); // list could be reused
            foreach (var upsert in entities) {
                var entity = objectMapper.Read<T>(upsert.value);
                var key     = EntityKeyTMap.GetKey(entity);
                upserts.Add(new Upsert<TKey, T>(key, entity));
            }
            return upserts;
        }

        internal override void AddDeletes  (List<JsonKey> ids) {
            foreach (var id in ids) {
                TKey    key      = KeyConvert.IdToKey(id);
                Deletes.Add(new Delete<TKey>(key));
            }
            changeInfo.deletes += ids.Count;
        }
        
        internal override void AddPatches(List<JsonEntity> entityPatches, FlioxClient client) {
            GetKeysFromEntities (client, keyName, entityPatches);
            for (int n = 0; n < entityPatches.Count; n++) {
                var     entityPatch = entityPatches[n];
                TKey    key         = KeyConvert.IdToKey(entityPatch.key);
                var     patch       = new Patch<TKey>(key, entityPatch.value);
                Patches.Add(patch);
            }
            changeInfo.merges += entityPatches.Count;
        }
        
        internal override void ApplyChangesToInternal  (EntitySet entitySet) {
            var set = (EntitySet<TKey, T>)entitySet;
            ApplyChangesTo(set);
        }
        
        /// <summary> Apply the container changes to the given <paramref name="entitySet"/> </summary>
        public ApplyResult<TKey,T> ApplyChangesTo(EntitySet<TKey, T> entitySet, Change change = Change.All) {
            applyInfos.Clear();
            if (Count == 0)
                return new ApplyResult<TKey,T>(applyInfos);
            var client = entitySet.intern.store;
            var localCreates    = rawCreates;
            if ((change & Change.create) != 0 && localCreates.Count > 0) {
                GetKeysFromEntities (client, keyName, localCreates);
                entitySet.SyncPeerEntities(localCreates, objectMapper, applyInfos);
            }
            var localUpserts    = rawUpserts;
            if ((change & Change.upsert) != 0 && localUpserts.Count > 0) {
                GetKeysFromEntities (client, keyName, localUpserts);
                entitySet.SyncPeerEntities(localUpserts, objectMapper, applyInfos);
            }
            if ((change & Change.merge) != 0) {
                entitySet.PatchPeerEntities(Patches, objectMapper, applyInfos);
            }
            if ((change & Change.delete) != 0) {
                entitySet.DeletePeerEntities(Deletes, applyInfos);
            }
            return new ApplyResult<TKey,T>(applyInfos);
        }
        
        private static void GetKeysFromEntities(FlioxClient client, string keyName, List<JsonEntity> entities) {
            var processor   = client._intern.EntityProcessor();
            var count       = entities.Count;
            for (int n = 0; n < count; n++) {
                var entity  = entities[n];
                if (!processor.GetEntityKey(entity.value, keyName, out JsonKey key, out string error))
                    throw new InvalidOperationException($"CreateEntityKeys() error: {error}");
                entities[n] = new JsonEntity(key, entity.value);
            }
        }
    }
    
    internal abstract class ChangeCallback {
        internal abstract void InvokeCallback(Changes entityChanges, EventContext context);
    }
    
    internal sealed class GenericChangeCallback<TKey, T> : ChangeCallback where T : class
    {
        private  readonly   ChangeSubscriptionHandler<TKey, T>   handler;
        
        internal GenericChangeCallback (ChangeSubscriptionHandler<TKey, T> handler) {
            this.handler = handler;
        }
        
        internal override void InvokeCallback(Changes entityChanges, EventContext context) {
            var changes = (Changes<TKey,T>)entityChanges;
            handler(changes, context);
        }
    }
}