// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Is throw when accessing the <b>Result</b> of an un-synced task. 
    /// </summary>
    public sealed class TaskNotSyncedException : Exception
    {
        internal TaskNotSyncedException(string message) : base (message) { }
    }
    
    /// <summary>
    /// Is thrown if calling a method on a task which was already executed by <see cref="FlioxClient.SyncTasks"/>
    /// </summary>
    public sealed class TaskAlreadySyncedException : Exception
    {
        internal TaskAlreadySyncedException(string message) : base (message) { }
    }
    
    /// <summary>
    /// Is thrown when accessing the <b>Result</b> of a synced task which returned an <see cref="SyncTask.Error"/>
    /// </summary>
    public sealed class TaskResultException : Exception
    {
        public readonly     TaskError       error;
        
        internal TaskResultException(TaskError error) : base(error.GetMessage(true)) {
            this.error      = error;
        }
    }

    /// <summary>
    /// Is thrown in case invocation of <see cref="FlioxClient.SyncTasks"/> failed entirely. E.g. a connection issue. 
    /// </summary>
    public sealed class SyncTasksException : Exception
    {
        public readonly     IReadOnlyList<SyncTask>  failed;

        internal SyncTasksException(ErrorResponse errorResponse, ListOne<SyncTask> failed)
            : base(SyncResult.GetMessage(errorResponse, failed))
        {
            this.failed = failed;
        }
    }
}