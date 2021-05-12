// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
namespace Friflo.Json.Flow.Sync
{
    public class TaskErrorResult : TaskResult
    {
        public              TaskErrorResultType type;
        public              string              message;

        internal override   TaskType            TaskType => TaskType.Error;
        public   override   string              ToString() => $"type: {type}, message: {message}";
    }
    
    public enum TaskErrorResultType {
        Undefined,          // Prevent implicit initialization of underlying value 0 to a valid value (UnhandledException) 
        UnhandledException,
        DatabaseError
    }
}