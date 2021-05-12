// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal struct TaskErrorInfo
    {
        internal    SyncError                           TaskError { get; private set; }
        
        // used sorted dictionary to ensure stable (and repeatable) order of errors
        internal    IDictionary<string, EntityError>    EntityErrors { get; private set; }
        
        internal    bool                                HasErrors => TaskError != null;
        public      override string                     ToString() => GetMessage();

        internal TaskErrorInfo(TaskError taskError) {
            TaskError       = new SyncError(taskError);
            EntityErrors    = null;
        }

        internal void AddEntityError(EntityError error) {
            if (EntityErrors == null) {
                EntityErrors = new SortedDictionary<string, EntityError>();
                TaskError = new SyncError(EntityErrors);
            }
            EntityErrors.Add(error.id, error);
        }
        
        internal string GetMessage() {
            var taskError = TaskError;
            if (EntityErrors == null) {
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
    
    // todo rename to TaskError
    public class SyncError {
        public   readonly   TaskErrorType                       type;
        public   readonly   string                              message;
        public   readonly   IDictionary<string, EntityError>    entityErrors;
       
        private static readonly IDictionary<string, EntityError> NoErrors = new EmptyDictionary<string, EntityError>();

        internal SyncError(TaskError error) {
            type            = error.type;
            message         = error.message;
            entityErrors    = NoErrors;
        }
        
        internal SyncError(IDictionary<string, EntityError> entityErrors) {
            this.entityErrors   = entityErrors ?? throw new ArgumentException("entityErrors must not be null");
            type                = TaskErrorType.EntityErrors;
            message             = "Task failed by entity errors";
        }
        
        public   override   string                              ToString() {
            if (type == TaskErrorType.EntityErrors) {
                return $"type: {type}, message: {message}, entityErrors: {entityErrors.Count}";
            }
            return $"type: {type}, message: {message}";
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
}