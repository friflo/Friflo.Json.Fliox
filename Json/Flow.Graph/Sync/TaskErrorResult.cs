// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Database;

namespace Friflo.Json.Flow.Sync
{
    public class TaskErrorResult : TaskResult
    {
        public              TaskErrorResultType type;
        public              string              message;
        public              string              stacktrace;

        internal override   TaskType            TaskType => TaskType.error;
        public   override   string              ToString() => $"type: {type}, message: {message}";
    }
    
    /// <summary>Describe the type of a <see cref="TaskErrorResult"/></summary>
    public enum TaskErrorResultType {
        None,
        /// <summary>
        /// Inform about an unhandled exception in a <see cref="EntityContainer"/> implementation which need to be fixed.
        /// More information at <see cref="EntityDatabase.ExecuteSync"/>.
        /// </summary>
        UnhandledException,
        
        /// <summary>
        /// Inform about an error when accessing a database.
        /// E.g. the access is currently not available or accessing a missing table.
        /// </summary>
        DatabaseError,
        
        InvalidTask,
        
        SyncError
    }
}