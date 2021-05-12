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
        internal abstract   string      Label   { get; }
        internal abstract   TaskState   State   { get; }
        
        private static readonly IDictionary<string, EntityError> NoErrors = new EmptyDictionary<string, EntityError>();  
        
        public              bool        Success { get {
            if (State.IsSynced())
                return !State.Error.HasErrors;
            throw new TaskNotSyncedException($"SyncTask.Success requires Sync(). {Label}");
        }}

        // return error as method - not as property to avoid flooding debug view with properties.
        // error is also visible via State.Error
        /// <returns>The error caused that task failed. Returns never null</returns>
        public SyncError   GetTaskError() {
            if (State.IsSynced())
                return State.Error.TaskError;
            throw new TaskNotSyncedException($"SyncTask.GetTaskError() requires Sync(). {Label}");
        }
        
        /// <returns>The entities caused that task failed. Otherwise null</returns>
        public IDictionary<string, EntityError>   GetEntityErrors() {
            if (State.IsSynced()) {
                var errors = State.Error.EntityErrors;
                if (errors != null)
                    return errors;
                return NoErrors;
            }
            throw new TaskNotSyncedException($"SyncTask.GetEntityErrors() requires Sync(). {Label}");
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