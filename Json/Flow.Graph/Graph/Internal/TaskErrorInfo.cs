// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal struct TaskErrorInfo
    {
        internal    TaskError                               TaskError { get; private set; }
        
        // used sorted dictionary to ensure stable (and repeatable) order of errors
        internal    SortedDictionary<string, EntityError>   EntityErrors { get; private set; }
        
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