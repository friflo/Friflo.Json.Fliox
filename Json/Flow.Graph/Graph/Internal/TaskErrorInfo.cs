// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal struct TaskErrorInfo
    {
        internal    SyncError                           TaskError { get; private set; }
        
        internal    bool                                HasErrors => TaskError != null;
        public      override string                     ToString() => GetMessage();

        internal TaskErrorInfo(TaskErrorResult taskError) {
            TaskError       = new SyncError(taskError);
        }

        internal void AddEntityError(EntityError error) {
            if (TaskError == null) {
                var entityErrors = new SortedDictionary<string, EntityError>();
                TaskError = new SyncError(entityErrors);
            }
            TaskError.entityErrors.Add(error.id, error);
        }
        
        internal string GetMessage() {
            var taskError = TaskError;
            if (TaskError.type != SyncErrorType.EntityErrors) {
                return $"Task failed. type: {taskError.type}, message: {taskError.message}";
            }
            var sb = new StringBuilder();
            var errors = TaskError.entityErrors;
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

        public override string ToString() => Synced ? Error.HasErrors ? $"synced - errors: {Error.TaskError.entityErrors.Count}" : "synced" : "not synced";
    }
}