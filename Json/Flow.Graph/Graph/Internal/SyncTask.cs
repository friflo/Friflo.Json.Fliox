// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    public abstract class SyncTask
    {
        public              bool            Success  => State.IsSynced() ? !State.Error.HasErrors : throw new TaskNotSyncedException($"task requires Sync(). {Label}");  
        public              TaskErrorInfo   Error    => State.IsSynced() ?  State.Error           : throw new TaskNotSyncedException($"task requires Sync(). {Label}");
        
        internal abstract   string      Label  { get; }
        internal abstract   TaskState   State { get; }

        internal bool IsOk(string syncError, out Exception e) {
            if (State.Error.HasErrors) {
                e = new TaskResultException(State.Error);
                return false;
            }
            if (State.IsSynced()) {
                e = null;
                return true;
            }
            e = new TaskNotSyncedException($"{syncError} requires Sync(). {Label}");
            return false;
        }
        
        internal Exception AlreadySyncedError() {
            return new TaskAlreadySyncedException($"Task already synced. {Label}");
        }
    }

    internal struct TaskState
    {
        internal bool           Synced { private get; set; }
        internal TaskErrorInfo  Error  { get; private set; }

        internal bool           IsSynced() { return Synced; }

        internal void SetError(TaskErrorInfo error) {
            Error  = error;
            Synced = true;
        }

        public override string ToString() => Synced ? Error.HasErrors ? $"synced - errors: {Error.EntityErrors.Count}" : "synced" : "not synced";
    }
    
    public struct TaskErrorInfo
    {
        public      TaskError                               TaskError { get; private set; }
        
        // used sorted dictionary to ensure stable (and repeatable) order of errors
        public      SortedDictionary<string, EntityError>   EntityErrors { get; private set; }
        
        internal    bool                                    HasErrors => TaskError != null || EntityErrors != null;

        internal TaskErrorInfo(TaskError taskError) {
            TaskError       = taskError;
            EntityErrors    = null;
        }

        internal void AddError(EntityError error) {
            if (EntityErrors == null)
                EntityErrors = new SortedDictionary<string, EntityError>();
            EntityErrors.Add(error.id, error);
        }
    }
    
    
}