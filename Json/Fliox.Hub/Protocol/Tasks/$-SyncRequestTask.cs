// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Polymorphic base type for all tasks.<br/>
    /// All tasks fall into two categories:<br/>
    /// <b>container operations</b> like: create, read, upsert, delete, query, ...<br/>
    /// <b>database operation</b> like sending commands or messages
    /// </summary>
    [Discriminator("task", "task type")]
    [PolymorphType(typeof(CreateEntities),          "create")]
    [PolymorphType(typeof(UpsertEntities),          "upsert")]
    [PolymorphType(typeof(ReadEntities),            "read")]
    [PolymorphType(typeof(QueryEntities),           "query")]
    [PolymorphType(typeof(AggregateEntities),       "aggregate")]
    [PolymorphType(typeof(MergeEntities),           "merge")]
    [PolymorphType(typeof(DeleteEntities),          "delete")]
    [PolymorphType(typeof(SendMessage),             "msg")]
    [PolymorphType(typeof(SendCommand),             "cmd")]
    [PolymorphType(typeof(CloseCursors),            "closeCursors")]
    [PolymorphType(typeof(SubscribeChanges),        "subscribeChanges")]
    [PolymorphType(typeof(SubscribeMessage),        "subscribeMessage")]
    [PolymorphType(typeof(ReserveKeys),             "reserveKeys")]
    public abstract class SyncRequestTask
    {
                    public      JsonValue       info;
        [Ignore]    internal    SyncTaskIntern  intern;

        
        public      abstract   Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext);
        // todo make abstract when SyncRequestTask are implemented
        public      abstract   SyncTaskResult       Execute     (EntityDatabase database, SyncResponse response, SyncContext syncContext); // => throw new NotImplementedException();
        
        public      abstract   TaskType         TaskType { get; }
        public      abstract   string           TaskName { get; }
        /// Return true in case a task is a 'No Operation' task. E.g an upsert without entities
        public      virtual    bool             IsNop()   => false;

        public      override   string           ToString() => TaskName;
        
        public virtual bool PreExecute(in PreExecute execute) {
            var isSync              = execute.db.IsSyncTask(this, execute);
            intern.executionType    = isSync ? ExecutionType.Sync : ExecutionType.Async;
            return isSync;
        }

        internal static TaskErrorResult TaskError(TaskExecuteError error) {
            return new TaskErrorResult (error.type, error.message);   
        }
        
        internal static TaskErrorResult InvalidTaskError(string error) {
            // error = $"{message} {TaskType} ({TaskName})";
            return new TaskErrorResult (TaskErrorType.InvalidTask, error);   
        }
        
        internal static TaskErrorResult MissingField(string field) {
            return InvalidTaskError($"missing field: {field}");   
        }
        
        internal static TaskErrorResult MissingContainer() {
            return InvalidTaskError($"missing field: container");   
        }
        
        internal static TaskErrorResult ContainerNotFound() {
            return InvalidTaskError($"container not found");   
        }
        
        internal static TaskErrorResult InvalidTask(string error) {
            return InvalidTaskError(error);
        }
        
        public static TaskErrorResult PermissionDenied(string message) {
            // message = $"{message} {TaskType} ({TaskName})";
            return new TaskErrorResult (TaskErrorType.PermissionDenied, message);
        }

        internal static bool ValidReferences(List<References> references, out TaskErrorResult error) {
            if (references == null) {
                error = null;
                return true;
            }
            foreach (var reference in references) {
                if (reference.selector == null) {
                    error = InvalidTaskError("missing reference selector");
                    return false;
                }
                if (reference.container.IsNull()) {
                    error =  InvalidTaskError("missing reference container");
                    return false;
                }
                var subReferences = reference.references;
                if (subReferences != null) {
                    if (!ValidReferences(subReferences, out error))
                        return false;
                }
            }
            error = null;
            return true;
        }
        
        public bool ContainsEntityChange (Change change, in ShortString container, in JsonKey id) {
            switch (TaskType) {
                case TaskType.create:   if ((change & Change.create) != 0)  break;   return false;
                case TaskType.upsert:   if ((change & Change.upsert) != 0)  break;   return false;
                case TaskType.merge:    if ((change & Change.merge)  != 0)  break;   return false;
                case TaskType.delete:   if ((change & Change.delete) != 0)  break;   return false;
                default:
                    return false;
            }
            return ContainsEntity(container, new HashSet<JsonKey>(JsonKey.Equality) { id });
        }
        
        private bool ContainsEntity (in ShortString container, HashSet<JsonKey> ids) {
            switch (TaskType) {
                case TaskType.read:
                    var read = (ReadEntities) this;
                    if (!read.container.IsEqual(container))
                        return false;
                    foreach (var readIds in read.ids.GetReadOnlySpan()) {
                        if (ids.Contains(readIds)) {
                            return true;
                        }
                    }
                    return false;
                case TaskType.create:
                    var create = (CreateEntities) this;
                    if (!create.container.IsEqual(container))
                        return false;
                    foreach (var entity in create.entities) {
                        if (ids.Contains(entity.key)) {
                            return true;
                        }
                    }
                    return false;
                case TaskType.upsert:
                    var upsert = (UpsertEntities) this;
                    if (!upsert.container.IsEqual(container))
                        return false;
                    foreach (var entity in upsert.entities) {
                        if (ids.Contains(entity.key)) {
                            return true;
                        }
                    }
                    return false;
                case TaskType.merge:
                    var merge = (MergeEntities) this;
                    if (!merge.container.IsEqual(container))
                        return false;
                    foreach (var patch in merge.patches) {
                        if (ids.Contains(patch.key)) {
                            return true;
                        }
                    }
                    return false;
                case TaskType.delete:
                    var delete = (DeleteEntities) this;
                    if (!delete.container.IsEqual(container))
                        return false;
                    foreach (var id in delete.ids.GetReadOnlySpan()) {
                        if (ids.Contains(id)) {
                            return true;
                        }
                    }
                    return false;
            }
            return false;
        }
    }
    
    /// <summary>
    /// Contain fields assigned in <see cref="FlioxHub.InitSyncRequest"/> by using the public fields of the <see cref="SyncRequestTask"/>
    /// </summary>
    internal struct SyncTaskIntern
    {
        internal    int             index;
        /// <summary>true if task can be executed synchronous. Otherwise false</summary>
        internal    ExecutionType   executionType;
        /// <summary>cached JSON of this <see cref="SyncRequestTask"/> instance serialized as JSON</summary>
        internal    JsonValue?      json;
        internal    SyncTask        syncTask;
        
        internal SyncTaskIntern(SyncTask syncTask) {
            this.syncTask   = syncTask;
            index           = 0;
            executionType   = default;
            json            = null;
        }
    }
    
    public readonly struct PreExecute
    {
        public readonly EntityDatabase  db;
        public readonly SharedEnv       env;
        public readonly bool            executeSync;
        
        internal  PreExecute (EntityDatabase db, SharedEnv env, bool executeSync) {
            this.db             = db;
            this.env            = env;
            this.executeSync    = executeSync;
        }
    }
}