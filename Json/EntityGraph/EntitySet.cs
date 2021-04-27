// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.EntityGraph.Internal;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.EntityGraph
{
    // --------------------------------------- EntitySet ---------------------------------------
    public abstract class EntitySet
    {
        internal  readonly  string  name;
        
        internal  abstract  SyncSet Sync       { get;  }
        internal  abstract  SetInfo SetInfo   { get;  }
        
        public    abstract  int     LogSetChanges();
        internal  abstract  void    SyncEntities        (ContainerEntities containerResults);
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
            var info = new SetInfo { peers = peers.Count };
            sync.SetTaskInfo(ref info);
            return info;
        }}

        public EntitySet(EntityStore store) : base (typeof(T).Name) {
            Type type = typeof(T);
            store._intern.setByType[type]       = this;
            store._intern.setByName[type.Name]  = this;
            container   = store._intern.database.GetContainer(name);
            intern      = new SetIntern<T>(store);
            sync        = new SyncSet<T>(this);
        }

        internal PeerEntity<T> CreatePeer (T entity) {
            if (peers.TryGetValue(entity.id, out PeerEntity<T> peer)) {
                if (peer.entity != entity)
                    throw new InvalidOperationException("");
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
        
        public ReadTask<T> Read(string id) {
            if (id == null)
                throw new InvalidOperationException($"EntitySet.Read() id must not be null. EntitySet: {name}");
            return sync.Read(id);
        }

        public QueryTask<T> Query(Expression<Func<T, bool>> filter) {
            if (filter == null)
                throw new InvalidOperationException($"EntitySet.Query() filter must not be null. EntitySet: {name}");
            var op = Operation.FromFilter(filter);
            return sync.QueryFilter(op);
        }
        
        public QueryTask<T> QueryByFilter(FilterOperation filter) {
            if (filter == null)
                throw new InvalidOperationException($"EntitySet.QueryByFilter() filter must not be null. EntitySet: {name}");
            return sync.QueryFilter(filter);
        }
        
        public QueryTask<T> QueryAll() {
            var all = Operation.FilterTrue;
            return sync.QueryFilter(all);
        }

        public CreateTask<T> Create(T entity) {
            if (entity == null)
                throw new InvalidOperationException($"EntitySet.Create() entity must not be null. EntitySet: {name}");
            if (entity.id == null)
                throw new InvalidOperationException($"EntitySet.Create() entity.id must not be null. EntitySet: {name}");
            return sync.Create(entity);
        }
        
        public DeleteTask Delete(string id) {
            if (id == null)
                throw new InvalidOperationException($"EntitySet.Delete() id must not be null. EntitySet: {name}");
            return sync.Delete(id);
        }
        
        public DeleteTask Delete(T entity) {
            if (entity == null)
                throw new InvalidOperationException($"EntitySet.Delete() entity must not be null. EntitySet: {name}");
            if (entity.id == null)
                throw new InvalidOperationException($"EntitySet.Delete() entity.id must not be null. EntitySet: {name}");
            return sync.Delete(entity.id);
        }

        public override int LogSetChanges() {
            return sync.LogSetChanges(peers);
        }

        public int LogEntityChanges(T entity) {
            if (entity == null)
                throw new InvalidOperationException($"EntitySet.LogEntityChanges() entity must not be null. EntitySet: {name}");
            if (entity.id == null)
                throw new InvalidOperationException($"EntitySet.LogEntityChanges() entity.id must not be null. EntitySet: {name}");
            return sync.LogEntityChanges(entity);
        }

        internal override void SyncEntities(ContainerEntities containerResults) {
            foreach (var entity in containerResults.entities) {
                var id = entity.Key;
                var peer = GetPeerById(id);
                var read = peer.read;
                var json = entity.Value.value.json;
                if (json != null && "null" != json) {
                    intern.jsonMapper.ReadTo(json, peer.entity);
                    peer.SetPatchSource(intern.jsonMapper.Read<T>(json));
                    if (read != null) {
                        read.result = peer.entity;
                        read.synced = true;
                    }
                } else {
                    peer.SetPatchSourceNull();
                    if (read != null) {
                        read.result = null;
                        read.synced = true;
                    }
                }
                peer.assigned = true;
                peer.read = null;
            }
        }

        internal override void ResetSync() {
            sync = new SyncSet<T>(this);
        }
    }
}