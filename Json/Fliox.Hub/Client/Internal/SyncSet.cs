// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal abstract class SyncSetBase <T> : SyncSet where T : class
    {
        internal abstract SubscribeChanges  SubscribeChanges(SubscribeChangesTask<T>    sub,    in CreateTaskContext context);
        internal abstract CreateEntities    CreateEntities  (CreateTask<T>              create, in CreateTaskContext context);
        internal abstract UpsertEntities    UpsertEntities  (UpsertTask<T>              upsert, in CreateTaskContext context);
    }

    /// Multiple instances of this class can be created when calling <see cref="FlioxClient.SyncTasks"/> without
    /// awaiting the result. Each instance is mapped to a <see cref="SyncRequest"/> / <see cref="SyncResponse"/> instance.
    internal sealed partial class SyncSet<TKey, T> : SyncSetBase<T> where T : class
    {
        private static readonly KeyConverter<TKey>  KeyConvert      = KeyConverter.GetConverter<TKey>();

        // --- internal fields
        internal  readonly  EntitySet<TKey, T>  set;
        internal  readonly  List<SyncTask>      tasks  = new List<SyncTask>();

        internal  override  EntitySet           EntitySet => set;
        public    override  string              ToString()  => "";

        internal SyncSet(EntitySet<TKey, T> set) {
            this.set = set;
        }

        // --- Read
        internal ReadTask<TKey, T> Read() {
            var read = new ReadTask<TKey, T>(this);
            tasks.Add(read);
            return read;
        }

        // --- Query
        internal QueryTask<TKey, T> QueryFilter(FilterOperation filter) {
            var query = new QueryTask<TKey, T>(filter, set.intern.store, this);
            tasks.Add(query);
            return query;
        }

        internal CloseCursorsTask CloseCursors(IEnumerable<string> cursors) {
            var closeCursor = new CloseCursorsTask(cursors, this);
            tasks.Add(closeCursor);
            return closeCursor;
        }

        // --- Aggregate
        internal CountTask<T> CountFilter(FilterOperation filter) {
            var aggregate   = new CountTask<T>(filter, this);
            tasks.Add(aggregate);
            return  aggregate;
        }

        // --- SubscribeChanges
        internal SubscribeChangesTask<T> SubscribeChangesFilter(Change change, FilterOperation filter) {
            var subscribeChanges = new SubscribeChangesTask<T>(this);
            var changes = change.ChangeToList();
            subscribeChanges.Set(changes, filter);
            tasks.Add(subscribeChanges);
            return subscribeChanges;
        }

        // --- ReserveKeys
        internal ReserveKeysTask<TKey, T> ReserveKeys(int count) {
            var reserveKeys = new ReserveKeysTask<TKey,T>(count, this);
            tasks.Add(reserveKeys);
            return reserveKeys;
        }

        // --- Create
        private CreateTask<T> CreateCreateTask() {
            var upsert = set.createBuffer.Get();
            if (upsert != null)
                return upsert;
            return new CreateTask<T>(new List<T>(), set, this);
        }
        
        internal CreateTask<T> Create(T entity) {
            if (set.intern.autoIncrement) {
                //  set.NewEntities().Add(entity);
                //  Autos().Add(entity);
                //  var create1 = new CreateTask<T>(new List<T>{entity}, set, this);
                //  CreateTasks().Add(create1);
                //  return create1;
            }
            var create  = CreateCreateTask();
            create.Add(entity);
            var peer    = set.CreatePeer(entity);
            create.AddPeer(peer, PeerState.Create);
            tasks.Add(create);
            return create;
        }

        internal CreateTask<T> CreateRange(ICollection<T> entities) {
            var create  = CreateCreateTask();
            create.AddRange(entities);
            foreach (var entity in entities) {
                var peer = set.CreatePeer(entity);
                create.AddPeer(peer, PeerState.Create);
            }
            tasks.Add(create);
            return create;
        }

        // --- Upsert
        private UpsertTask<T> CreateUpsertTask() {
            var upsert = set.upsertBuffer.Get();
            if (upsert != null)
                return upsert;
            return new UpsertTask<T>(new List<T>(), set, this);
        }
        
        internal UpsertTask<T> Upsert(T entity) {
            var upsert  = CreateUpsertTask();
            upsert.Add(entity);
            var peer    = set.CreatePeer(entity);
            upsert.AddPeer(peer, PeerState.Upsert);
            tasks.Add(upsert);
            return upsert;
        }

        internal UpsertTask<T> UpsertRange(ICollection<T> entities) {
            var upsert  = CreateUpsertTask();
            upsert.AddRange(entities);
            foreach (var entity in entities) {
                var peer = set.CreatePeer(entity);
                upsert.AddPeer(peer, PeerState.Upsert);
            }
            tasks.Add(upsert);
            return upsert;
        }

        // --- Delete
        private DeleteTask<TKey, T> CreateDelete() {
            var delete = set.deleteBuffer.Get();
            if (delete != null)
                return delete;
            return new DeleteTask<TKey, T>(new List<TKey>(), this);
        }
        
        internal DeleteTask<TKey, T> Delete(TKey key) {
            var delete  = CreateDelete();
            delete.Add(key);
            tasks.Add(delete);
            return delete;
        }

        internal DeleteTask<TKey, T> DeleteRange(ICollection<TKey> keys) {
            var delete = CreateDelete();
            delete.AddRange(keys);
            tasks.Add(delete);
            return delete;
        }

        internal DeleteAllTask<TKey, T> DeleteAll() {
            var deleteAll = new DeleteAllTask<TKey, T>(this);
            tasks.Add(deleteAll);
            return deleteAll;
        }
        
        // --- Patch
        // - detect patches
        internal void AddDetectPatches(DetectPatchesTask<TKey,T> detectPatchesTask) {
            tasks.Add(detectPatchesTask);
        }

        // Deprecated comment - preserve for now to remember history of Ref{TKey,T} and Tracer
        //   In case the given entity is already <see cref="Peer{T}.created"/> or <see cref="Peer{T}.updated"/> trace
        //   the entity to find changes in referenced entities in <see cref="Ref{TKey,T}"/> fields of the given entity.
        //   In these cases <see cref="Map.RefMapper{TKey,T}.Trace"/> add untracked entities (== have no <see cref="Peer{T}"/>)
        //   which is not already assigned)
        internal void DetectPeerPatches(TKey key, Peer<T> peer, DetectPatchesTask<TKey,T> detectPatchesTask, ObjectMapper mapper) {
            if ((peer.state & (PeerState.Create | PeerState.Upsert)) != 0) {
                // tracer.Trace(peer.Entity);
                return;
            }
            var patchSource = peer.PatchSource;
            if (patchSource.IsNull())
                return;
            var entity  = peer.Entity;
            var differ  = set.intern.store._intern.ObjectDiffer();
            var source  = mapper.Read<T>(patchSource);
            var diff    = differ.GetDiff(source, entity, DiffKind.DiffArrays);
            if (diff == null)
                return;
            var jsonDiff    = set.intern.store._intern.JsonMergeWriter();
            var mergePatch  = jsonDiff.WriteEntityMergePatch(diff, entity);
            
            SetNextPatchSource(peer, mapper); // todo next patch source need to be set on Synchronize()
            
            detectPatchesTask.AddPatch(mergePatch, key, entity);
            // tracer.Trace(entity);
        }

        // ----------------------------------- create task methods -----------------------------------
        internal ReserveKeys ReserveKeys(ReserveKeysTask<TKey,T> reserveKeys) {
            return new ReserveKeys {
                container   = set.name,
                count       = reserveKeys.count,
                syncTask    = reserveKeys
            };
        }

        internal override CreateEntities CreateEntities(CreateTask<T> create, in CreateTaskContext context) {
            var creates = create.peers;
            var entries = new List<JsonEntity>(creates.Count);
            var writer  = context.mapper;
            writer.Pretty           = set.intern.writePretty;
            writer.WriteNullMembers = set.intern.writeNull;

            foreach (var createPair in creates) {
                var peer    = createPair.Value;
                T entity    = peer.Entity;
                var value   = writer.WriteAsValue(entity);
                entries.Add(new JsonEntity(peer.id, value));
            }
            return new CreateEntities {
                container       = set.name,
                keyName         = SyncKeyName(set.GetKeyName()),
                entities        = entries,
                reservedToken   = new Guid(), // todo
                syncTask        = create
            };

        }

        internal override UpsertEntities UpsertEntities(UpsertTask<T> upsert, in CreateTaskContext context) {
            var peers               = upsert.peers;
            var writer              = context.mapper;
            writer.Pretty           = set.intern.writePretty;
            writer.WriteNullMembers = set.intern.writeNull;
            var entries = new List<JsonEntity>(peers.Count);

            foreach (var upsertPair in peers) {
                var peer    = upsertPair.Value;
                T entity    = peer.Entity;
                var value   = writer.WriteAsValue(entity);
                entries.Add(new JsonEntity(peer.id, value));
            }
            return new UpsertEntities {
                container   = set.name,
                keyName     = SyncKeyName(set.GetKeyName()),
                entities    = entries,
                syncTask    = upsert  
            };
        }

        internal SyncRequestTask ReadEntities(ReadTask<TKey,T> read) {
            List<References> references = null;
            if (read.relations.subRelations.Count > 0) {
                references = new List<References>(read.relations.subRelations.Count);
                AddReferences(references, read.relations.subRelations);
            }
            var ids = new List<JsonKey>(read.result.Keys.Count);
            foreach (var key in read.result.Keys) {
                var id = KeyConvert.KeyToId(key);
                ids.Add(id);
            }
            return new ReadEntities {
                container   = set.name,
                keyName     = SyncKeyName(set.GetKeyName()),
                isIntKey    = IsIntKey(set.IsIntKey()),
                ids         = ids,
                references  = references,
                syncTask    = read
            };
        }

        internal QueryEntities QueryEntities(QueryTask<TKey, T> query, in CreateTaskContext context) {
            var subRelations = query.relations.subRelations;
            List<References> references = null;
            if (subRelations.Count > 0) {
                references = new List<References>(subRelations.Count);
                AddReferences(references, subRelations);
            }
            var filterTree  = FilterToJson(query.filter, context.mapper);
            return new QueryEntities {
                container   = set.name,
                keyName     = SyncKeyName(set.GetKeyName()),
                isIntKey    = IsIntKey(set.IsIntKey()),
                filterTree  = filterTree,
                filter      = query.filterLinq,
                references  = references,
                limit       = query.limit,
                maxCount    = query.maxCount,
                cursor      = query.cursor,
                syncTask    = query 
            };
        }

        internal override AggregateEntities AggregateEntities(AggregateTask aggregate, in CreateTaskContext context) {
            var filterTree  = FilterToJson(aggregate.filter, context.mapper);
            return new AggregateEntities {
                container   = set.name,
                type        = aggregate.Type,
            //  keyName     = SyncKeyName(set.GetKeyName()),
            //  isIntKey    = IsIntKey(set.IsIntKey()),
                filterTree  = filterTree,
                filter      = aggregate.filterLinq,
                syncTask    = aggregate 
            };
        }
        
        private static JsonValue FilterToJson(FilterOperation filter, ObjectMapper mapper) {
            var jsonFilter  = mapper.writer.Write(filter);
            return new JsonValue(jsonFilter);
        }

        internal override CloseCursors CloseCursors(CloseCursorsTask closeCursor) {
            return new CloseCursors {
                container   = set.name,
                cursors     = closeCursor.cursors,
                syncTask    = closeCursor 
            };
        }
        
        internal MergeEntities MergeEntities(DetectPatchesTask<TKey,T> detectPatches) {
            var patches = detectPatches.Patches;
            if (patches.Count == 0) {
                detectPatches.state.Executed = true;
            }
            var list = new List<JsonEntity>(patches.Count);
            foreach (var patch in patches) {
                list.Add(new JsonEntity(patch.entityPatch));
            }
            return new MergeEntities {
                container   = set.name,
                keyName     = SyncKeyName(set.GetKeyName()),
                patches     = list,
                syncTask    = detectPatches
            };
        }

        internal DeleteEntities DeleteEntities(DeleteTask<TKey,T> deleteTask) {
            var deletes = deleteTask.keys;
            var ids     = new List<JsonKey>(deletes.Count);
            foreach (var key in deletes) {
                var id = KeyConvert.KeyToId(key);
                ids.Add(id);
            }
            return new DeleteEntities {
                container   = set.name,
                ids         = ids,
                syncTask    = deleteTask 
            };
        }

        internal DeleteEntities DeleteAll(DeleteAllTask<TKey,T> deleteTask) {
           return new DeleteEntities {
                container   = set.name,
                all         = true,
                syncTask    = deleteTask 
            };
        }

        internal override SubscribeChanges SubscribeChanges(SubscribeChangesTask<T> sub, in CreateTaskContext context) {
            var filter = sub.filter.Linq;
            return new SubscribeChanges {
                container   = set.name,
                filter      = filter,
                changes     = sub.changes,
                syncTask    = sub 
            };
        }

        // ----------------------------------- helper methods -----------------------------------
        private static void AddReferences(List<References> references, SubRelations relations) {
            foreach (var readRefs in relations) {
                var queryReference = new References {
                    container   = readRefs.Container,
                    keyName     = SyncKeyName(readRefs.KeyName),
                    isIntKey    = IsIntKey(readRefs.IsIntKey),
                    selector    = readRefs.Selector
                };
                references.Add(queryReference);
                var subRefsMap = readRefs.SubRelations;
                if (subRefsMap.Count > 0) {
                    queryReference.references = new List<References>(subRefsMap.Count);
                    AddReferences(queryReference.references, subRefsMap);
                }
            }
        }

        private static void SetNextPatchSource(Peer<T> peer, ObjectMapper mapper) {
            var json   = mapper.writer.WriteAsValue(peer.Entity);
            peer.SetNextPatchSource(json);
        }

        internal void SetTaskInfo(ref SetInfo info) {
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
    }
}