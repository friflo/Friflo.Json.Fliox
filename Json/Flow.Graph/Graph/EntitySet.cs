// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Transform;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Transform.Query;

namespace Friflo.Json.Flow.Graph
{
    // --------------------------------------- EntitySet ---------------------------------------
    public abstract class EntitySet
    {
        internal  readonly  string  name;
        
        internal  abstract  SyncSet Sync      { get; }
        internal  abstract  SetInfo SetInfo   { get; }

        internal static readonly QueryPath RefQueryPath = new RefQueryPath();
        
        internal  abstract  void    LogSetChangesInternal (LogTask logTask);
        internal  abstract  void    SyncContainerEntities (ContainerEntities containerResults);
        internal  abstract  void    ResetSync           ();

        protected EntitySet(string name) {
            this.name = name;
        }
    }

    internal readonly struct SetIntern<T> where T : Entity
    {
        internal readonly   TypeMapper<T>       typeMapper;
        internal readonly   ObjectMapper        jsonMapper;
        internal readonly   ObjectPatcher       objectPatcher;
        internal readonly   Tracer              tracer;
        internal readonly   EntityStore         store;


        internal SetIntern(EntityStore store) {
            jsonMapper      = store._intern.jsonMapper;
            typeMapper      = (TypeMapper<T>)store._intern.typeStore.GetTypeMapper(typeof(T));
            objectPatcher   = store._intern.objectPatcher;
            tracer          = new Tracer(store._intern.typeCache, store);
            this.store      = store;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class EntitySet<T> : EntitySet where T : Entity
    {
        // Keep all utility related fields of EntitySet in SetIntern to enhance debugging overview.
        // Reason:  EntitySet is extended by application which is mainly interested in following fields while debugging:
        //          peers, Sync, name, container & store 
        internal readonly   SetIntern<T>                        intern;
        
        /// key: <see cref="PeerEntity{T}.entity"/>.id          Note: must be private by all means
        private  readonly   Dictionary<string, PeerEntity<T>>   peers       = new Dictionary<string, PeerEntity<T>>();
        
        private  readonly   EntityContainer                     container; // not used - only for debugging ergonomics
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal            SyncSet<T>                          sync;
        
        internal override   SyncSet                             Sync => sync;
        public   override   string                              ToString() => SetInfo.ToString();
        
        internal override   SetInfo                             SetInfo { get {
            var info = new SetInfo (name) { peers = peers.Count };
            sync.SetTaskInfo(ref info);
            return info;
        }}

        
        // --------------------------------------- public interface --------------------------------------- 
        // --- Read
        public ReadTask<T> Read() {
            return sync.Read();
        }

        // --- Query
        public QueryTask<T> Query(Expression<Func<T, bool>> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.Query() filter must not be null. EntitySet: {name}");
            var op = Operation.FromFilter(filter, RefQueryPath);
            return sync.QueryFilter(op);
        }
        
        public QueryTask<T> QueryByFilter(FilterOperation filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.QueryByFilter() filter must not be null. EntitySet: {name}");
            return sync.QueryFilter(filter);
        }
        
        public QueryTask<T> QueryAll() {
            var all = Operation.FilterTrue;
            return sync.QueryFilter(all);
        }

        // --- Create
        public CreateTask<T> Create(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Create() entity must not be null. EntitySet: {name}");
            if (entity.id == null)
                throw new ArgumentException($"EntitySet.Create() entity.id must not be null. EntitySet: {name}");
            return sync.Create(entity);
        }
        
        public CreateRangeTask<T> CreateRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.CreateRange() entity must not be null. EntitySet: {name}");
            foreach (var entity in entities) {
                if (entity.id == null)
                    throw new ArgumentException($"EntitySet.CreateRange() entity.id must not be null. EntitySet: {name}");
            }
            return sync.CreateRange(entities);
        }
        
        // --- Update
        public UpdateTask<T> Update(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Update() entity must not be null. EntitySet: {name}");
            if (entity.id == null)
                throw new ArgumentException($"EntitySet.Update() entity.id must not be null. EntitySet: {name}");
            return sync.Update(entity);
        }
        
        public UpdateRangeTask<T> UpdateRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.UpdateRange() entity must not be null. EntitySet: {name}");
            foreach (var entity in entities) {
                if (entity.id == null)
                    throw new ArgumentException($"EntitySet.UpdateRange() entity.id must not be null. EntitySet: {name}");
            }
            return sync.UpdateRange(entities);
        }

        // --- Delete
        public DeleteTask<T> Delete(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Delete() entity must not be null. EntitySet: {name}");
            return Delete(entity.id);
        }

        public DeleteTask<T> Delete(string id) {
            if (id == null)
                throw new ArgumentException($"EntitySet.Delete() id must not be null. EntitySet: {name}");
            return sync.Delete(id);
        }
        
        public DeleteRangeTask<T> DeleteRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.DeleteRange() entities must not be null. EntitySet: {name}");
            var ids = entities.Select(e => e.id).ToList();
            return DeleteRange(ids);
        }
        
        public DeleteRangeTask<T> DeleteRange(ICollection<string> ids) {
            if (ids == null)
                throw new ArgumentException($"EntitySet.DeleteRange() ids must not be null. EntitySet: {name}");
            foreach (var id in ids) {
                if (id == null)
                    throw new ArgumentException($"EntitySet.DeleteRange() id must not be null. EntitySet: {name}");
            }
            return sync.DeleteRange(ids);
        }

        // --- Log changes -> create patches
        public LogTask LogSetChanges() {
            var logTask = intern.store.sync.CreateLog();
            sync.LogSetChanges(peers, logTask);
            return logTask;
        }

        public LogTask LogEntityChanges(T entity) {
            var logTask = intern.store.sync.CreateLog();
            if (entity == null)
                throw new ArgumentException($"EntitySet.LogEntityChanges() entity must not be null. EntitySet: {name}");
            if (entity.id == null)
                throw new ArgumentException($"EntitySet.LogEntityChanges() entity.id must not be null. EntitySet: {name}");
            sync.LogEntityChanges(entity, logTask);
            return logTask;
        }
        

        // ------------------------------------------- internals -------------------------------------------
        internal override void LogSetChangesInternal(LogTask logTask) {
            sync.LogSetChanges(peers, logTask);
        }
        
        public EntitySet(EntityStore store) : base (typeof(T).Name) {
            Type type = typeof(T);
            store._intern.setByType[type]       = this;
            store._intern.setByName[type.Name]  = this;
            container   = store._intern.database.GetOrCreateContainer(name);
            intern      = new SetIntern<T>(store);
            sync        = new SyncSet<T>(this);
        }

        internal PeerEntity<T> CreatePeer (T entity) {
            if (peers.TryGetValue(entity.id, out PeerEntity<T> peer)) {
                if (peer.entity != entity)
                    throw new ArgumentException($"Another entity with same id is already tracked. id: {entity.id}");
                return peer;
            }
            peer = new PeerEntity<T>(entity);
            peers.Add(entity.id, peer);
            return peer;
        }
        
        internal void DeletePeer (string id) {
            peers.Remove(id);
        }
        
        internal PeerEntity<T> GetPeerByRef(Ref<T> reference) {
            string id = reference.id;
            PeerEntity<T> peer = reference.GetPeer();
            if (peer == null) {
                var entity = reference.GetEntity();
                if (entity != null)
                    peer = CreatePeer(entity);
                else
                    peer = GetPeerById(id);
            }
            return peer;
        }

        internal PeerEntity<T> GetPeerById(string id) {
            if (peers.TryGetValue(id, out PeerEntity<T> peer)) {
                return peer;
            }
            var entity = (T)intern.typeMapper.CreateInstance();
            peer = new PeerEntity<T>(entity);
            peer.entity.id = id;
            peers.Add(id, peer);
            return peer;
        }
        
        // --- EntitySet
        internal override void SyncContainerEntities(ContainerEntities containerResults) {
            foreach (var entity in containerResults.entities) {
                var id = entity.Key;
                var value = entity.Value;
                if (value.Error != null) {
                    // id & container are not serialized as they are redundant data.
                    // Infer their values from containing dictionary & EntitySet<>
                    value.Error.id          = id;
                    value.Error.container   = name;
                    continue;
                }

                var json = value.Json;
                var peer = GetPeerById(id);
                if (json != null && "null" != json) {
                    var reader = intern.jsonMapper.reader;
                    reader.ReadTo(json, peer.entity);
                    if (reader.Success) {
                        peer.SetPatchSource(reader.Read<T>(json));
                    } else {
                        var error = new EntityError(EntityErrorType.ParseError, name, id, reader.Error.msg.ToString());
                        containerResults.entities[id].SetError(error);
                    }
                } else {
                    peer.SetPatchSourceNull();
                }
                peer.assigned = true;
            }
        }

        internal override void ResetSync() {
            sync = new SyncSet<T>(this);
        }
    }
}