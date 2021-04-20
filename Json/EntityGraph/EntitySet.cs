// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.EntityGraph
{
    // --------------------------------------- EntitySet ---------------------------------------
    public abstract class EntitySet
    {
        internal  readonly  string          name;
        
        internal  abstract  EntitySetSync   Sync { get;  }
        
        internal  abstract  void            ReadReferenceResult (ReadReference task, ReadReferenceResult  result, List<string> parentIds, ReadRefTaskMap map);

        public    abstract  int             LogSetChanges();
        internal  abstract  void            SyncEntities        (ContainerEntities containerResults);
        internal  abstract  void            ResetSync           ();

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
            jsonMapper      = store.intern.jsonMapper;
            typeMapper      = (TypeMapper<T>)store.intern.typeStore.GetTypeMapper(typeof(T));
            objectPatcher   = store.intern.objectPatcher;
            tracer          = new Tracer(store.intern.typeCache, store);
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
        internal            EntitySetSync<T>                    sync; // todo: intended to create a new instance after calling Sync()
        
        internal override   EntitySetSync                       Sync => sync;
        
        public EntitySet(EntityStore store) : base (typeof(T).Name) {
            Type type = typeof(T);
            store.intern.setByType[type]       = this;
            store.intern.setByName[type.Name]  = this;
            container   = store.intern.database.GetContainer(name);
            intern      = new SetIntern<T>(store);
            sync        = new EntitySetSync<T>(this);
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
        
        internal PeerEntity<T> GetPeerByRef(Ref<T> reference) {
            string id = reference.Id;
            PeerEntity<T> peer = reference.peer;
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
            return sync.Read(id);
        }

        public QueryTask<T> Query(Expression<Func<T, bool>> filter) {
            var op = Operation.FromFilter(filter);
            return QueryByFilter(op);
        }
        
        public QueryTask<T> QueryByFilter(FilterOperation filter) {
            return sync.QueryFilter(filter);
        }
        
        public QueryTask<T> QueryAll() {
            var all = Operation.FilterTrue;
            return QueryByFilter(all);
        }

        public CreateTask<T> Create(T entity) {
            return sync.Create(entity);
        }

        public override int LogSetChanges() {
            return sync.LogSetChanges(peers);
        }

        public int LogEntityChanges(T entity) {
            return sync.LogEntityChanges(entity);
        }

        internal override void ReadReferenceResult(ReadReference task, ReadReferenceResult result, List<string> parentIds, ReadRefTaskMap map) {
            foreach (var parentId in parentIds) {
                var reference = map.readRefs[parentId];
                if (reference.singleResult) {
                    var singleRef = (ReadRefTask<T>) reference;
                    if (result.ids.Count != 1)
                        throw new InvalidOperationException("Expect exactly one reference");
                    var id = result.ids[0];
                    var peer = GetPeerById(id);
                    singleRef.id        = id;
                    singleRef.entity    = peer.entity;
                    singleRef.synced    = true;
                } else {
                    var multiRef = (ReadRefsTask<T>) reference;
                    multiRef.synced = true;
                    for (int o = 0; o < result.ids.Count; o++) {
                        var id = result.ids[o];
                        var peer = GetPeerById(id);
                        var readRef = new ReadRefTask<T>(reference.parentId, reference.parentSet, reference.label) {
                            id      = id,
                            entity  = peer.entity,
                            synced  = true
                        };
                        multiRef.results.Add(readRef);
                    }
                }
            }
        }

        internal override void SyncEntities(ContainerEntities containerResults) {
            foreach (var entity in containerResults.entities) {
                var id = entity.Key;
                var peer = GetPeerById(id);
                var read = peer.read;
                var json = entity.Value.value.json;
                if (json != null && "null" != json) {
                    intern.jsonMapper.ReadTo(json, peer.entity);
                    peer.patchReference = intern.jsonMapper.Read<T>(json);
                    peer.assigned = true;
                    if (read != null) {
                        read.result = peer.entity;
                        read.synced = true;
                    }
                } else {
                    peer.patchReference = null;
                    if (read != null) {
                        read.result = null;
                        read.synced = true;
                    }
                }
                peer.read = null;
            }
        }

        internal override void ResetSync() {
            sync = new EntitySetSync<T>(this);
        }
    }
}