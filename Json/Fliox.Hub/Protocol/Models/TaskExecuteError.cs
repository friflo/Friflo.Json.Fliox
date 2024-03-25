// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    public interface ITaskResultError
    {
        /// In case a task fails its <see cref="TaskExecuteError.message"/> is assigned to <see cref="TaskErrorResult.message"/>
        [Ignore]    TaskExecuteError        Error { get; set;  }
    }
    
    /// <summary>
    /// Note: A <see cref="TaskExecuteError"/> is never serialized.
    /// Its fields are assigned to <see cref="TaskErrorResult"/> which instead is used for serialization of errors.
    /// </summary>
    public sealed class TaskExecuteError
    {
        [Ignore]    internal  readonly  TaskErrorType   type;
        [Ignore]    public    readonly  string          message;

        public      override            string          ToString() => message;
        
        public TaskExecuteError(string message) {
            this.type       = TaskErrorType.DatabaseError;
            this.message    = message;
        }
        public TaskExecuteError(TaskErrorType type, string message) {
            this.type       = type;
            this.message    = message;
        }
    }
}