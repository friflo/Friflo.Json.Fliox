// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Flow.Graph.Internal;

using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Flow.Graph
{
    public abstract class SyncTask
    {
                                    internal            string      name;
                                    internal            string      GetLabel() => name ?? Details;
        [DebuggerBrowsable(Never)]  public    abstract  string      Details { get; }
        [DebuggerBrowsable(Never)]  internal  abstract  TaskState   State   { get; }
        
        public   override   string      ToString()  => GetLabel();

        public              bool        Success { get {
            if (State.IsSynced())
                return !State.Error.HasErrors;
            throw new TaskNotSyncedException($"SyncTask.Success requires Sync(). {GetLabel()}");
        }}

        /// <summary>The error caused the task failing. Return null if task was successful - <see cref="Success"/> == true</summary>
        public              TaskError   Error { get {
            if (State.IsSynced())
                return State.Error.TaskError;
            throw new TaskNotSyncedException($"SyncTask.Error requires Sync(). {GetLabel()}");
        } }

        internal bool IsOk(string method, out Exception e) {
            if (State.IsSynced()) {
                if (!State.Error.HasErrors) {
                    e = null;
                    return true;
                }
                e = new TaskResultException(State.Error.TaskError);
                return false;
            }
            e = new TaskNotSyncedException($"{method} requires Sync(). {GetLabel()}");
            return false;
        }
        
        internal Exception AlreadySyncedError() {
            return new TaskAlreadySyncedException($"Task already synced. {GetLabel()}");
        }

        internal virtual void AddFailedTask(List<SyncTask> failed) {
            if (!State.Error.HasErrors)
                return;
            failed.Add(this);
        }
    }
    
    public static class SyncTaskExtension
    {
        public static T TaskName<T> (this T task, string name) where T : SyncTask {
            task.name = name;
            return task;
        }
    }
}