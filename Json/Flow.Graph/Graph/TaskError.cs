// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    public enum TaskErrorType
    {
        Undefined, // Prevent implicit initialization of underlying value 0 to a valid value (UnhandledException) 
        UnhandledException,
        DatabaseError,
        EntityErrors // Is set only by Flow.Graph implementation - not by Flow.Database
    }
    
    public class TaskError {
        public   readonly   TaskErrorType                       type;
        public   readonly   string                              message;
        /// The entities caused that task failed. Return empty dictionary in case of no entity errors. Is never null.
        public   readonly   IDictionary<string, EntityError>    entityErrors;
       
        private static readonly IDictionary<string, EntityError> NoErrors = new EmptyDictionary<string, EntityError>();

        internal TaskError(TaskErrorResult error) {
            type            = TaskToSyncError(error.type);
            message         = error.message;
            entityErrors    = NoErrors;
        }

        internal TaskError(IDictionary<string, EntityError> entityErrors) {
            this.entityErrors   = entityErrors ?? throw new ArgumentException("entityErrors must not be null");
            type                = TaskErrorType.EntityErrors;
            message             = "Task failed by entity errors";
        }
        
        private static TaskErrorType TaskToSyncError(TaskErrorResultType type) {
            switch (type) {
                case TaskErrorResultType.UnhandledException:  return TaskErrorType.UnhandledException;
                case TaskErrorResultType.DatabaseError:       return TaskErrorType.DatabaseError;
            }
            throw new ArgumentException($"cant convert error type: {type}");
        }
        
        public   override   string                              ToString() {
            if (type == TaskErrorType.EntityErrors) {
                return $"type: {type}, message: {message}, entityErrors: {entityErrors.Count}";
            }
            return $"type: {type}, message: {message}";
        }

        internal string GetMessage() {
            var sb = new StringBuilder();
            AppendAsText("", sb, 10);
            return sb.ToString();
        }

        internal void AppendAsText(string prefix, StringBuilder sb, int maxEntityErrors) {
            if (type != TaskErrorType.EntityErrors) {
                sb.Append("Task failed. type: ");
                sb.Append(type);
                sb.Append(", message: ");
                sb.Append(message);
                return;
            }
            var errors = entityErrors;
            sb.Append("Task failed by entity errors. Count: ");
            sb.Append(errors.Count);
            int n = 0;
            foreach (var errorPair in errors) {
                var error = errorPair.Value;
                sb.Append('\n');
                sb.Append(prefix);
                if (n++ == maxEntityErrors) {
                    sb.Append("...");
                    break;
                }
                sb.Append("| ");
                error.AppendAsText(sb);
            }
        }
    }
}