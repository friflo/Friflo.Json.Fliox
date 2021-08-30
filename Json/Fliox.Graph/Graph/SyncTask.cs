// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Graph.Internal;

using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Graph
{
    public abstract class SyncTask
    {
                                    internal            string      taskName;
                                    internal            string      GetLabel() => taskName ?? Details;
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
        /// <summary>
        /// An arbitrary name which can be assigned to a task. Typically the name of the variable the task is assigned to.
        /// The <see cref="name"/> is used to simplify finding a <see cref="SyncTask"/> in the source code while debugging.
        /// It also simplifies finding a <see cref="TaskError"/> by its <see cref="TaskError.Message"/>
        /// or a <see cref="TaskResultException"/> by its <see cref="Exception.Message"/>.
        /// The library itself doesnt use the <see cref="name"/> internally - its purpose is only to enhance debugging
        /// or post-mortem debugging of application code.
        /// </summary>
        public static T TaskName<T> (this T task, string name) where T : SyncTask {
            task.taskName = name;
            return task;
        }
    }
}