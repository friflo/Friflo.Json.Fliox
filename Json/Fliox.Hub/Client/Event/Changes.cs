// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
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
        public              int                         Count       => changeInfo.Count;
        /// <summary> number of changes per mutation type: creates, upserts, deletes and patches </summary>
        public              ChangeInfo                  ChangeInfo  => changeInfo;
        /// <summary> name of the container the changes are referring to </summary>
        public    abstract  string                      Container   { get; }
        /// <summary> raw JSON values of created container entities </summary>
        public              IReadOnlyList<JsonValue>    RawCreates  => rawCreates;
        /// <summary> raw JSON values of upserted container entities </summary>
        public              IReadOnlyList<JsonValue>    RawUpserts  => rawUpserts;
        
        [DebuggerBrowsable(Never)]  internal            bool            added;
        [DebuggerBrowsable(Never)]  internal            ChangeInfo      changeInfo;
        [DebuggerBrowsable(Never)]  internal  readonly  List<JsonValue> rawCreates  = new List<JsonValue>();
        [DebuggerBrowsable(Never)]  internal  readonly  List<JsonValue> rawUpserts  = new List<JsonValue>();
        [DebuggerBrowsable(Never)]  internal  readonly  List<ApplyInfo> applyInfos  = new List<ApplyInfo>();

        internal  abstract  Type        GetEntityType();
        internal  abstract  void        Clear       ();
        internal  abstract  void        AddDeletes  (List<JsonKey> ids);
        internal  abstract  void        AddPatches  (List<EntityPatch> patches);
        internal  abstract  ApplyResult ApplyChangesToInternal  (EntitySet entitySet);
        
        public ApplyResult ApplyChangesToContainer(FlioxClient client, string container) {
            var set = client.GetEntitySet(container);
            if (!client._intern.TryGetSetByName(container, out _))
                return new ApplyResult();
            return ApplyChangesToInternal(set);
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
        public              List<T>             Creates         => GetCreates();
        /// <summary> return the entities upserted in a container </summary>
        public              List<T>             Upserts         => GetUpserts();
        /// <summary> return the keys of removed container entities </summary>
        public              List<TKey>          Deletes { get; } = new List<TKey>();
        /// <summary> return patches applied to container entities </summary>
        public              List<Patch<TKey>>   Patches { get; } = new List<Patch<TKey>>();
        public    override  string              ToString()      => FormatToString();       
        public    override  string              Container       { get; }
        internal  override  Type                GetEntityType() => typeof(T);
        
        [DebuggerBrowsable(Never)] private          List<T>         creates;
        [DebuggerBrowsable(Never)] private          List<T>         upserts;
        [DebuggerBrowsable(Never)] private readonly ObjectMapper    objectMapper;

        
        private static readonly KeyConverter<TKey>  KeyConvert = KeyConverter.GetConverter<TKey>();

        /// <summary> called via <see cref="SubscriptionProcessor.GetChanges"/> </summary>
        internal Changes(EntitySet<TKey, T> entitySet, ObjectMapper mapper) {
            Container       = entitySet.name;
            objectMapper    = mapper;
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
        
        private List<T> GetCreates() {
            if (creates != null)
                return creates;
            // create entities on demand
            var entities = rawCreates;
            creates = new List<T>(entities.Count); // list could be reused
            foreach (var create in entities) {
                var entity = objectMapper.Read<T>(create);
                creates.Add(entity);
            }
            return creates;
        }
        
        private List<T> GetUpserts() {
            if (upserts != null)
                return upserts;
            // create entities on demand
            var entities = rawUpserts;
            upserts = new List<T>(entities.Count); // list could be reused
            foreach (var upsert in entities) {
                var entity = objectMapper.Read<T>(upsert);
                upserts.Add(entity);
            }
            return upserts;
        }

        internal override void AddDeletes  (List<JsonKey> ids) {
            foreach (var id in ids) {
                TKey    key      = KeyConvert.IdToKey(id);
                Deletes.Add(key);
            }
            changeInfo.deletes += ids.Count;
        }
        
        internal override void AddPatches(List<EntityPatch> entityPatches) {
            foreach (var entityPatch in entityPatches) {
                var     id          = entityPatch.id;
                TKey    key         = KeyConvert.IdToKey(id);
                var     patch       = new Patch<TKey>(key, id, entityPatch.patches);
                Patches.Add(patch);
            }
            changeInfo.patches += entityPatches.Count;
        }
        
        internal override ApplyResult ApplyChangesToInternal  (EntitySet entitySet) {
            var set = (EntitySet<TKey, T>)entitySet;
            return ApplyChangesTo(set);
        }
        
        /// <summary> Apply the container changes to the given <paramref name="entitySet"/> </summary>
        public ApplyResult ApplyChangesTo(EntitySet<TKey, T> entitySet, Change change = Change.All) {
            applyInfos.Clear();
            if (Count == 0)
                return new ApplyResult(applyInfos);
            var client = entitySet.intern.store;
            var localCreates    = rawCreates;
            if ((change & Change.create) != 0 && localCreates.Count > 0) {
                var entityKeys = GetKeysFromEntities (client, entitySet.GetKeyName(), localCreates);
                entitySet.SyncPeerEntities(entityKeys, localCreates, objectMapper, applyInfos);
            }
            var localUpserts    = rawUpserts;
            if ((change & Change.upsert) != 0 && localUpserts.Count > 0) {
                var entityKeys = GetKeysFromEntities (client, entitySet.GetKeyName(), localUpserts);
                entitySet.SyncPeerEntities(entityKeys, localUpserts, objectMapper, applyInfos);
            }
            if ((change & Change.patch) != 0) {
                entitySet.PatchPeerEntities(Patches, objectMapper);
            }
            if ((change & Change.delete) != 0) {
                entitySet.DeletePeerEntities(Deletes);
            }
            return new ApplyResult(applyInfos);
        }
        
        private static List<JsonKey> GetKeysFromEntities(FlioxClient client, string keyName, List<JsonValue> entities) {
            var processor   = client._intern.EntityProcessor();
            var keys        = new List<JsonKey>(entities.Count);
            foreach (var entity in entities) {
                if (!processor.GetEntityKey(entity, keyName, out JsonKey key, out string error))
                    throw new InvalidOperationException($"CreateEntityKeys() error: {error}");
                keys.Add(key);
            }
            return keys;
        }
    }
    
    public readonly struct Patch<TKey> {
        internal  readonly  JsonKey             id;
        public    readonly  List<JsonPatch>     patches;
        public    readonly  TKey                key;

        public  override    string              ToString() => key.ToString();
        
        public Patch(TKey key, in JsonKey id, List<JsonPatch> patches) {
            this.id         = id;
            this.key        = key;
            this.patches    = patches;
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
    
    [Flags]
    public enum ApplyInfoType {
        EntityCreate,
        EntityUpdate,
        ParseError
    }
    
    public readonly struct ApplyInfo {
        public readonly ApplyInfoType   type;
        public readonly JsonKey         key;
        public readonly JsonValue       value;
        
        internal ApplyInfo(ApplyInfoType type, in JsonKey key, in JsonValue value) {
            this.type   = type;
            this.key    = key;
            this.value  = value;
        }
    }

    public readonly struct ApplyResult {
        public readonly IReadOnlyList<ApplyInfo> applyInfos;
        
        internal ApplyResult(IReadOnlyList<ApplyInfo> applyInfos) {
            this.applyInfos = applyInfos;
        }
    }
}