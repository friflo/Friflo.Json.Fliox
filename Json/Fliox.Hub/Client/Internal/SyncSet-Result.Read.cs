// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal partial class SyncSet<TKey, T>
    {
        // --- read one entity
        private void ReadEntityResult(ReadEntities task, ReadEntitiesResult result, FindTask<TKey, T> read, ObjectMapper mapper) {
            if (result.Error != null) {
                var taskError = SyncRequestTask.TaskError(result.Error);
                SetReadOneTaskError(read, taskError);
                return;
            }
            var  entityErrorInfo = new TaskErrorInfo();
            if (result.entities.Type == EntitiesType.Values) {
                AddReadEntity(ref entityErrorInfo, result, read, mapper.reader);
            } else {
                AddReadObject(ref entityErrorInfo, result, read);
            }
            // A ReadTask is set to error if at least one of its JSON results has an error.
            if (entityErrorInfo.HasErrors) {
                read.state.SetError(entityErrorInfo);
                // SetReadTaskError(read, entityErrorInfo); <- must not be called
                return;
            }
            read.state.Executed = true;
            AddReferencesResult(task.references, result.references, read.relations.subRelations, mapper.reader);
        }
        
        private static void SetReadOneTaskError(FindTask<TKey, T> read, TaskErrorResult taskError) {
            TaskErrorInfo error = new TaskErrorInfo(taskError);
            read.state.SetError(error);
            SetSubRelationsError(read.relations.subRelations, error);
        }
        
        // --- read multiple entities
        private void ReadEntitiesResult(ReadEntities task, ReadEntitiesResult result, ReadTask<TKey, T> read, ObjectMapper mapper) {
            if (result.Error != null) {
                var taskError = SyncRequestTask.TaskError(result.Error);
                SetReadTaskError(read, taskError);
                return;
            }
            var  entityErrorInfo = new TaskErrorInfo();
            if (result.entities.Type == EntitiesType.Values) {
                AddReadEntities(ref entityErrorInfo, result, read, mapper.reader);
            } else {
                AddReadObjects(ref entityErrorInfo, result, read);
            }
            // A ReadTask is set to error if at least one of its JSON results has an error.
            if (entityErrorInfo.HasErrors) {
                read.state.SetError(entityErrorInfo);
                // SetReadTaskError(read, entityErrorInfo); <- must not be called
                return;
            }
            read.state.Executed = true;
            AddReferencesResult(task.references, result.references, read.relations.subRelations, mapper.reader);
        }
        
        private static void SetReadTaskError(ReadTask<TKey, T> read, TaskErrorResult taskError) {
            TaskErrorInfo error = new TaskErrorInfo(taskError);
            read.state.SetError(error);
            SetSubRelationsError(read.relations.subRelations, error);
            foreach (var findTask in read.findTasks) {
                findTask.findState.SetError(error);
            }
        }

        // ---------------------------------------- add value / values  ----------------------------------------
        // SYNC_READ : read entity
        private void AddReadEntity(ref TaskErrorInfo entityErrorInfo, ReadEntitiesResult result, FindTask<TKey, T> read, ObjectReader reader)
        {
            var values  = set.GetReadResultValues(result);
            var id      = KeyConvert.KeyToId(read.key);
            var peer    = set.GetOrCreatePeerByKey(read.key, id);
            if (values.Length == 0) {
                peer.SetPatchSourceNull();
                peer.SetEntity(null);
                return;
            }
            ref var value   = ref values[0];
            if (!id.IsEqual(value.key)) {
                entityErrorInfo = new TaskErrorInfo(TaskErrorType.InvalidResponse, $"expect id {value.key}");
                return;
            }
            read.result = set.AddEntity(value, peer, reader, out var error);
            if (error == null) {
                return;
            }
            entityErrorInfo.AddEntityError(error);
        }
        
        // SYNC_READ : read entities
        private void AddReadEntities(ref TaskErrorInfo entityErrorInfo, ReadEntitiesResult result, ReadTask<TKey, T> read, ObjectReader reader)
        {
            var values      = set.GetReadResultValues(result);
            var readResult  = read.result;
            
            foreach (var value in values) {
                var id = KeyConvert.IdToKey(value.key);
                if (!readResult.ContainsKey(id)) {
                    continue;
                }
                var peer = set.GetOrCreatePeerByKey(id, value.key);
                readResult[id] = set.AddEntity(value, peer, reader, out var error);
                if (error == null) {
                    continue;
                }
                entityErrorInfo.AddEntityError(error);
            }
            // set all peers to null not found in result.entities
            foreach (var pair in readResult) {
                if (pair.Value != default) {
                    continue;
                }
                if (!set.TryGetPeerByKey(pair.Key, out var peer)) {
                    // peer.SetPatchSourceNull();
                    // peer.SetEntity(null);
                }
            }
            var taskError = entityErrorInfo.TaskError;
            foreach (var findTask in read.findTasks) {
                findTask.SetFindResult(read.result, taskError);
            }
        }
        
        // ---------------------------------------- add object / objects  ----------------------------------------
        // SYNC_READ : read object
        private void AddReadObject(ref TaskErrorInfo entityErrorInfo, ReadEntitiesResult result, FindTask<TKey, T> read)
        {
            var objects = result.entities.Objects;
            var id      = KeyConvert.KeyToId(read.key);
            var peer    = set.GetOrCreatePeerByKey(read.key, id);
            if (objects.Count == 0) {
                peer.SetEntity(null);
                return;
            }
            var entity  = (T)objects[0].entity;
            var objId   = EntityKeyTMap.GetId(entity);
            if (!id.IsEqual(objId)) {
                entityErrorInfo = new TaskErrorInfo(TaskErrorType.InvalidResponse, $"expect id {id}");
                return;
            }
            var current = peer.NullableEntity;
            if (current == null) {
                peer.SetEntity(entity);
                read.result = entity;
                return;
            }
            var typeMapper  = set.GetTypeMapper();
            typeMapper.MemberwiseCopy(entity, current);
            read.result = current;
        }
        
        // SYNC_READ : read objects
        private TaskErrorInfo AddReadObjects(ref TaskErrorInfo entityErrorInfo, ReadEntitiesResult result, ReadTask<TKey, T> read)
        {
            var objects     = result.entities.Objects;
            var readResult  = read.result;
            var typeMapper  = set.GetTypeMapper();
            foreach (var obj in objects)
            {
                var entity  = (T)obj.entity;
                var key     = EntityKeyTMap.GetKey(entity);
                if (!readResult.ContainsKey(key)) {
                    continue;
                }
                var id      = KeyConvert.KeyToId(key);
                var peer    = set.GetOrCreatePeerByKey(key, id);
                var current = peer.NullableEntity;

                if (current == null) {
                    peer.SetEntity(entity);
                    readResult[key] = entity;
                    continue;
                }
                typeMapper.MemberwiseCopy(entity, current);
                readResult[key] = current;
            }
            foreach (var findTask in read.findTasks) {
                findTask.SetFindResult(read.result, null);
            }
            return default;
        }
        
        // ---------------------------------------- query values ----------------------------------------
        private void QueryEntitiesResult(
            QueryEntities       task,
            QueryEntitiesResult queryResult,
            QueryTask<TKey, T>  query,
            ObjectReader        reader)
        {
            var entityErrorInfo = new TaskErrorInfo();
            query.sql           = queryResult.sql;
            query.resultCursor  = queryResult.cursor;
            var values          = set.GetQueryResultValues(queryResult);
            query.entities      = values;
            var results         = query.result = new List<T>(values.Length);
            foreach (var value in values)
            {
                var key     = KeyConvert.IdToKey(value.key);
                var peer    = set.GetOrCreatePeerByKey(key, value.key);
                var entity  = set.AddEntity(value, peer, reader, out var error);
                if (error == null) {
                    results.Add(entity);
                    continue;
                }
                entityErrorInfo.AddEntityError(error);
            }
            if (entityErrorInfo.HasErrors) {
                query.state.SetError(entityErrorInfo);
                SetSubRelationsError(query.relations.subRelations, entityErrorInfo);
                return;
            }
            AddReferencesResult(task.references, queryResult.references, query.relations.subRelations, reader);
            query.state.Executed = true;
        }
        
        // -------------------------------------- references values --------------------------------------
        private void AddReferencesResult(
            List<References>        references,
            List<ReferencesResult>  referencesResult,
            SubRelations            relations,
            ObjectReader            reader)
        {
            // in case (references != null &&  referencesResult == null) => no reference ids found for references 
            if (references == null || referencesResult == null)
                return;
            for (int n = 0; n < references.Count; n++) {
                References              reference    = references[n];
                ReferencesResult        refResult    = referencesResult[n];
                EntitySet               refContainer = set.client.GetSetByName(reference.container);
                ReadRelationsFunction   subRelation  = relations[reference.selector];
                if (refResult.error != null) {
                    var taskError       = new TaskErrorResult (TaskErrorType.DatabaseError, refResult.error);
                    var taskErrorInfo   = new TaskErrorInfo (taskError);
                    subRelation.state.SetError(taskErrorInfo);
                    continue;
                }
                var values = refContainer.AddReferencedEntities(refResult, reader);
                subRelation.SetResult(refContainer, values);
                // handle entity errors of subRef task
                var subRefError = subRelation.state.Error;
                if (subRefError.HasErrors) {
                    if (subRefError.TaskError.type != TaskErrorType.EntityErrors)
                        throw new InvalidOperationException("Expect subRef Error.type == EntityErrors");
                    SetSubRelationsError(subRelation.SubRelations, subRefError);
                    continue;
                }
                subRelation.state.Executed = true;
                var subReferences = reference.references;
                if (subReferences != null) {
                    var readRefs = subRelation.SubRelations;
                    AddReferencesResult(subReferences, refResult.references, readRefs, reader);
                }
            }
        }

        private static void SetSubRelationsError(SubRelations relations, TaskErrorInfo taskErrorInfo) {
            foreach (var subRef in relations) {
                subRef.state.SetError(taskErrorInfo);
                SetSubRelationsError(subRef.SubRelations, taskErrorInfo);
            }
        }
    }
}