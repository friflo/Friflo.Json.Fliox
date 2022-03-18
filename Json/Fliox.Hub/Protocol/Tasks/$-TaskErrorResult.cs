// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task result -----------------------------------
    public sealed class TaskErrorResult : SyncTaskResult
    {
        [Fri.Required]  public  TaskErrorResultType type;
                        public  string              message;
                        public  string              stacktrace;

        internal override       TaskType            TaskType => TaskType.error;
        public   override       string              ToString() => $"type: {type}, message: {message}";
        
        public TaskErrorResult() {}
        public TaskErrorResult(TaskErrorResultType type, string message, string stacktrace = null) {
            this.type       = type;
            this.message    = message;
            this.stacktrace = stacktrace;
        }
    }
    
    /// <summary>Type of a task error used in <see cref="TaskErrorResult"/></summary>
    public enum TaskErrorResultType {
        /// HTTP status: 500
        None,
        /// <summary>
        /// maps to HTTP status: 500
        /// Inform about an unhandled exception in a <see cref="EntityContainer"/> implementation which need to be fixed.
        /// More information at <see cref="FlioxHub.ExecuteSync"/>.
        /// </summary>
        UnhandledException,
        /// <summary>
        /// maps to HTTP status: 500<br/>
        /// Inform about an error when accessing a database.<br/>
        /// E.g. the access is currently not available or accessing a missing table.
        /// </summary>
        DatabaseError,
        /// <summary>maps to HTTP status: 400<br/>
        /// Invalid query filter</summary>
        FilterError,
        /// <summary>maps to HTTP status: 400<br/>
        /// Schema validation of an entity failed</summary>
        ValidationError,
        /// <summary>maps to HTTP status: 400<br/>
        /// Execution of message / command failed caused by invalid input</summary>
        CommandError,
        /// <summary>maps to HTTP status: 400<br/>
        /// Invalid task. E.g. by using an invalid task parameter</summary>
        InvalidTask,
        /// <summary>maps to HTTP status: 501<br/>
        /// database message / command not implemented</summary>
        NotImplemented,
        /// <summary>maps to HTTP status: 403<br/>
        /// execution of container operation or database message / command not authorized</summary>
        PermissionDenied,
        /// <summary>maps to HTTP status: 500<br/>
        /// The entire <see cref="SyncRequest"/> containing a task failed 
        /// </summary>
        SyncError
    }
}