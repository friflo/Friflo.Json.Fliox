// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Protocol;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

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
                        public              int                 Count       => changeInfo.Count;
        /// <summary> number of changes per mutation type: creates, upserts, deletes and patches </summary>
                        public              ChangeInfo          ChangeInfo  => changeInfo;
        /// <summary> name of the container the changes are referring to </summary>
                        public    abstract  string              Container       { get; }
        
                        public    readonly  RawChanges          raw;
        // --- internal
        [Browse(Never)] public    abstract  ShortString         ContainerShort  { get; }
        [Browse(Never)] internal            bool                added;
        [Browse(Never)] internal            ChangeInfo          changeInfo;

        internal  abstract  void        Clear       ();
        internal  abstract  void        ApplyChangesToInternal  (Set entitySet);
        
        protected Changes() {
            raw = new RawChanges(null);
        }
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
                        public              List<Delete<TKey>>      Deletes         => GetDeletes();
        /// <summary> return patches applied to container entities </summary>
                        public              List<Patch<TKey>>       Patches         => GetPatches();
        
                        private readonly    List<ApplyInfo<TKey,T>> applyInfos  = new List<ApplyInfo<TKey,T>>();

                        public  override    string                  ToString()      => FormatToString();       
                        public  override    string                  Container       { get; }
        [Browse(Never)] public  override    ShortString             ContainerShort  { get; }
        
        [Browse(Never)] private             List<Create<TKey,T>>    creates;
        [Browse(Never)] private             List<Upsert<TKey,T>>    upserts;
        [Browse(Never)] private             List<Delete<TKey>>      deletes;
        [Browse(Never)] private             List<Patch<TKey>>       patches;
        [Browse(Never)] private readonly    SubscriptionIntern      intern;
        [Browse(Never)] private readonly    string                  keyName;
        
        private static  readonly    EntityKeyT<TKey, T> EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();

        
        private static readonly KeyConverter<TKey>  KeyConvert = KeyConverter.GetConverter<TKey>();

        /// <summary> called via <see cref="SubscriptionProcessor.GetChanges"/> </summary>
        internal Changes(Set<TKey, T> set, SubscriptionIntern intern) {
            keyName         = set.keyName;
            Container       = set.name;
            ContainerShort  = set.nameShort;
            this.intern     = intern;
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
            deletes = null;
            patches = null;
            
            raw.creates.Clear();
            raw.upserts.Clear();
            raw.deletes.Clear();
            raw.patches.Clear();
            //
            changeInfo.Clear();
        }
        
        private List<Create<TKey,T>> GetCreates() {
            if (creates != null)
                return creates;
            // create entities on demand
            var entities = raw.creates;
            creates     = new List<Create<TKey,T>>(entities.Count);         // list could be reused
            var mapper  = intern.objectMapper;
            foreach (var create in entities) {
                var entity  = mapper.Read<T>(create);
                var key     = EntityKeyTMap.GetKey(entity);
                creates.Add(new Create<TKey, T>(key, entity));
            }
            return creates;
        }
        
        private List<Upsert<TKey,T>> GetUpserts() {
            if (upserts != null)
                return upserts;
            // create entities on demand
            var entities = raw.upserts;
            upserts = new List<Upsert<TKey,T>>(entities.Count);         // list could be reused
            var mapper  = intern.objectMapper;
            foreach (var upsert in entities) {
                var entity  = mapper.Read<T>(upsert);
                var key     = EntityKeyTMap.GetKey(entity);
                upserts.Add(new Upsert<TKey, T>(key, entity));
            }
            return upserts;
        }

        private List<Delete<TKey>> GetDeletes  () {
            if (deletes != null)
                return deletes;
            var ids = raw.deletes;
            deletes = new List<Delete<TKey>>(ids.Count);                // list could be reused
            foreach (var id in ids) {
                TKey    key      = KeyConvert.IdToKey(id);
                deletes.Add(new Delete<TKey>(key));
            }
            return deletes;
        }
        
        private List<Patch<TKey>> GetPatches() {
            if (patches != null)
                return patches;
            var rawPatches      = raw.patches;
            patches             = new List<Patch<TKey>>(rawPatches.Count);  // list could be reused
            GetKeysFromEntities (rawPatches, intern.keys);
            for (int n = 0; n < rawPatches.Count; n++) {
                TKey    key     = KeyConvert.IdToKey(intern.keys[n]);
                var     patch   = new Patch<TKey>(key, rawPatches[n]);
                patches.Add(patch);
            }
            return patches;
        }
        
        internal override void ApplyChangesToInternal  (Set entitySet) {
            var set = (Set<TKey, T>)entitySet;
            ApplyChangesToInternal(set);
        }
        
        /// <summary> Apply the container changes to the given <paramref name="entitySet"/> </summary>
        public ApplyResult<TKey,T> ApplyChangesTo(EntitySet<TKey, T> entitySet, Change change = Change.All) {
            var instance = entitySet.GetInstance();
            FlioxClient.AssertTrackEntities(instance.client, nameof(ApplyChangesTo));
            return ApplyChangesToInternal(instance, change);
        }
        
        private ApplyResult<TKey,T> ApplyChangesToInternal(Set<TKey, T> set, Change change = Change.All) {
            applyInfos.Clear();
            if (Count == 0)
                return new ApplyResult<TKey,T>(applyInfos);
            var localCreates    = raw.creates;
            if ((change & Change.create) != 0 && localCreates.Count > 0) {
                GetKeysFromEntities (localCreates, intern.keys);
                set.SyncPeerEntities(localCreates, intern.keys, intern.objectMapper, applyInfos);
            }
            var localUpserts    = raw.upserts;
            if ((change & Change.upsert) != 0 && localUpserts.Count > 0) {
                GetKeysFromEntities (localUpserts, intern.keys);
                set.SyncPeerEntities(localUpserts, intern.keys, intern.objectMapper, applyInfos);
            }
            if ((change & Change.merge)  != 0 && raw.patches.Count > 0) {
                set.PatchPeerEntities(Patches, intern.objectMapper, applyInfos);
            }
            if ((change & Change.delete) != 0 && raw.deletes.Count > 0) {
                set.DeletePeerEntities(Deletes, applyInfos);
            }
            return new ApplyResult<TKey,T>(applyInfos);
        }
        
        private void GetKeysFromEntities(List<JsonValue> entities, List<JsonKey> keys) {
            var entityProcessor = intern.entityProcessor;
            var count           = entities.Count;
            keys.Clear();
            for (int n = 0; n < count; n++) {
                var entity  = entities[n];
                if (!entityProcessor.GetEntityKey(entity, keyName, out JsonKey key, out string error))
                    throw new InvalidOperationException($"CreateEntityKeys() error: {error}");
                keys.Add(key);
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