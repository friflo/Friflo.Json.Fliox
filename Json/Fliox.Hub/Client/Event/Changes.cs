// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
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

        internal  abstract  void        Clear       ();
        internal  abstract  void        AddDeletes  (List<JsonKey> ids);
        internal  abstract  void        AddPatches  (List<JsonValue> patches, FlioxClient client);
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
        public              List<T>             Creates         => GetCreates();
        /// <summary> return the entities upserted in a container </summary>
        public              List<T>             Upserts         => GetUpserts();
        /// <summary> return the keys of removed container entities </summary>
        public              List<TKey>          Deletes { get; } = new List<TKey>();
        /// <summary> return patches applied to container entities </summary>
        public              List<Patch<TKey>>   Patches { get; } = new List<Patch<TKey>>();
        
        internal  readonly  List<ApplyInfo<TKey,T>> applyInfos  = new List<ApplyInfo<TKey,T>>();

        public    override  string              ToString()      => FormatToString();       
        public    override  string              Container       { get; }
        
        [DebuggerBrowsable(Never)] private          List<T>         creates;
        [DebuggerBrowsable(Never)] private          List<T>         upserts;
        [DebuggerBrowsable(Never)] private readonly ObjectMapper    objectMapper;
        [DebuggerBrowsable(Never)] private readonly List<JsonKey>   keyBuffer;
        [DebuggerBrowsable(Never)] private readonly string          keyName;

        
        private static readonly KeyConverter<TKey>  KeyConvert = KeyConverter.GetConverter<TKey>();

        /// <summary> called via <see cref="SubscriptionProcessor.GetChanges"/> </summary>
        internal Changes(EntitySet<TKey, T> entitySet, ObjectMapper mapper, List<JsonKey> keyBuffer) {
            keyName         = entitySet.GetKeyName();
            Container       = entitySet.name;
            objectMapper    = mapper;
            this.keyBuffer  = keyBuffer;
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
        
        internal override void AddPatches(List<JsonValue> entityPatches, FlioxClient client) {
            GetKeysFromEntities (keyBuffer, client, keyName, entityPatches);
            for (int n = 0; n < entityPatches.Count; n++) {
                var     entityPatch = entityPatches[n];
                var     id          = keyBuffer[n];
                TKey    key         = KeyConvert.IdToKey(id);
                var     patch       = new Patch<TKey>(key, entityPatch);
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
                GetKeysFromEntities (keyBuffer, client, keyName, localCreates);
                entitySet.SyncPeerEntities(keyBuffer, localCreates, objectMapper, applyInfos);
            }
            var localUpserts    = rawUpserts;
            if ((change & Change.upsert) != 0 && localUpserts.Count > 0) {
                GetKeysFromEntities (keyBuffer, client, keyName, localUpserts);
                entitySet.SyncPeerEntities(keyBuffer, localUpserts, objectMapper, applyInfos);
            }
            if ((change & Change.merge) != 0) {
                entitySet.PatchPeerEntities(Patches, objectMapper, applyInfos);
            }
            if ((change & Change.delete) != 0) {
                entitySet.DeletePeerEntities(Deletes, applyInfos);
            }
            return new ApplyResult<TKey,T>(applyInfos);
        }
        
        private static void GetKeysFromEntities(List<JsonKey> keys, FlioxClient client, string keyName, List<JsonValue> entities) {
            keys.Clear();
            var processor   = client._intern.EntityProcessor();
            foreach (var entity in entities) {
                if (!processor.GetEntityKey(entity, keyName, out JsonKey key, out string error))
                    throw new InvalidOperationException($"CreateEntityKeys() error: {error}");
                keys.Add(key);
            }
        }
    }
    
    public readonly struct Patch<TKey> {
        public    readonly  JsonValue   patch;
        public    readonly  TKey        key;
        
        public  override    string      ToString() => key.ToString();
        
        public Patch(TKey key, JsonValue patch) {
            this.key        = key;
            this.patch      = patch;
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
        EntityCreated   = 0x01,
        EntityUpdated   = 0x02,
        EntityDeleted   = 0x04,
        EntityPatched   = 0x08,
        ParseError      = 0x80,
    }
    
    public readonly struct ApplyInfo<TKey, T> where T : class {
        public readonly ApplyInfoType   type;
        public readonly TKey            key;
        public readonly T               entity;
        public readonly JsonValue       rawEntity;

        public override string          ToString() => $"{type} key: {key}"; 

        internal ApplyInfo(ApplyInfoType type, TKey key, T entity, in JsonValue rawEntity) {
            this.type       = type;
            this.key        = key;
            this.entity     = entity;
            this.rawEntity  = rawEntity;
        }
    }

    public readonly struct ApplyResult<TKey, T> where T : class {
        public readonly IReadOnlyList<ApplyInfo<TKey,T>> applyInfos;
        
        public override string  ToString() => applyInfos != null ? $"Count: {applyInfos.Count}" : "error";
        
        internal ApplyResult(IReadOnlyList<ApplyInfo<TKey,T>> applyInfos) {
            this.applyInfos = applyInfos;
        }
    }
}