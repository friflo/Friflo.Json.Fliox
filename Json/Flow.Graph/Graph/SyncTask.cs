// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    public abstract class SyncTask
    {
        public              bool        Success  => State.IsSynced() ? !State.Error.HasErrors : throw new TaskNotSyncedException($"SyncTask.Success requires Sync(). {Label}");  
        
        internal abstract   string      Label  { get; }
        internal abstract   TaskState   State { get; }
        
        // return error as method - not as property to avoid flooding debug view with properties.
        // error is also visible via State.Error
        public TaskErrorInfo   GetError() {
            return State.IsSynced()
                ? State.Error
                : throw new TaskNotSyncedException($"SyncTask.Error requires Sync(). {Label}");
        }

        internal bool IsOk(string method, out Exception e) {
            if (State.Error.HasErrors) {
                e = new TaskResultException(State.Error);
                return false;
            }
            if (State.IsSynced()) {
                e = null;
                return true;
            }
            e = new TaskNotSyncedException($"{method} requires Sync(). {Label}");
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
        public      override string                         ToString() => GetMessage();

        internal TaskErrorInfo(TaskError taskError) {
            TaskError       = taskError;
            EntityErrors    = null;
        }

        internal void AddError(EntityError error) {
            if (EntityErrors == null)
                EntityErrors = new SortedDictionary<string, EntityError>();
            EntityErrors.Add(error.id, error);
        }
        
        internal string GetMessage() {
            var taskError = TaskError;
            if (taskError != null) {
                return $"Task failed. type: {taskError.type}, message: {taskError.message}";
            }
            var sb = new StringBuilder();
            var errors = EntityErrors;
            sb.Append("Task failed by entity errors. Count: ");
            sb.Append(errors.Count);
            int n = 0;
            foreach (var errorPair in errors) {
                var error = errorPair.Value;
                if (n++ == 10) {
                    sb.Append("\n...");
                    break;
                }
                sb.Append("\n| ");
                error.AppendAsText(sb);
            }
            return sb.ToString();
        }
    }
    
    
}