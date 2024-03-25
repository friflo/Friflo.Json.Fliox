// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Client.Internal
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
        internal bool           Executed       { private get; set; }
        internal TaskErrorInfo  Error          { get; private set; }

        internal bool           IsExecuted()   { return Executed; }

        internal void SetError(TaskErrorInfo error) {
            Error       = error;
            Executed   = true;
        }
        
        internal void SetInvalidResponse(string message) {
            Error  = new TaskErrorInfo(TaskErrorType.InvalidResponse, message);
            Executed = true;
        }

        public override string ToString() => Executed ? Error.HasErrors ? $"synced with error" : "synced" : "not synced";
    }
}