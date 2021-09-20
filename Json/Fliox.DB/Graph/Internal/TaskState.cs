// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph.Internal
{
    internal struct TaskErrorInfo
    {
        internal    TaskError           TaskError { get; private set; }
        
        internal    bool                HasErrors   => TaskError != null;
        public      override string     ToString()  => TaskError.GetMessage(true);

        internal TaskErrorInfo(TaskErrorResult taskError) {
            TaskError = new TaskError(taskError);
        }
        
        internal TaskErrorInfo(TaskErrorType type, string message) {
            TaskError = new TaskError(type, message);
        }

        internal void AddEntityError(EntityError error) {
            if (TaskError == null) {
                var entityErrors = new SortedDictionary<JsonKey, EntityError>(JsonKey.Comparer);
                TaskError = new TaskError(entityErrors);
            }
            TaskError.entityErrors.Add(error.id, error);
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
        
        internal void SetInvalidResponse(string message) {
            Error  = new TaskErrorInfo(TaskErrorType.InvalidResponse, message);
            Synced = true;
        }

        public override string ToString() => Synced ? Error.HasErrors ? $"synced with error" : "synced" : "not synced";
    }
}