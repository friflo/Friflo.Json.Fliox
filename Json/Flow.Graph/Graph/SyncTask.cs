// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Internal;
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
        public TaskError   GetTaskError() {
            if (State.IsSynced())
                return State.Error.TaskError;
            throw new TaskNotSyncedException($"SyncTask.Error requires Sync(). {Label}");
        }
        
        public SortedDictionary<string, EntityError>   GetEntityErrors() {
            if (State.IsSynced())
                return State.Error.EntityErrors;
            throw new TaskNotSyncedException($"SyncTask.Error requires Sync(). {Label}");
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
}