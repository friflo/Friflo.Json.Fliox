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
        
        public              bool        Success { get {
            if (State.IsSynced())
                return !State.Error.HasErrors;
            throw new TaskNotSyncedException($"SyncTask.Success requires Sync(). {Label}");
        }}

        /// <summary>The error caused the task failing. Return null if task was successful - <see cref="Success"/> == true</summary>
        public              TaskError   Error { get {
            if (State.IsSynced())
                return State.Error.TaskError;
            throw new TaskNotSyncedException($"SyncTask.GetTaskError() requires Sync(). {Label}");
        } }

        /// <returns>The entities caused that task failed. Return empty dictionary in case of no entity errors. Never returns null</returns>
        public IDictionary<string, EntityError>   GetEntityErrors() {
            if (State.IsSynced()) {
                return State.Error.TaskError.entityErrors;
            }
            throw new TaskNotSyncedException($"SyncTask.GetEntityErrors() requires Sync(). {Label}");
        }

        internal bool IsOk(string method, out Exception e) {
            if (State.Error.HasErrors) {
                e = new TaskResultException(State.Error.TaskError);
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