// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.DB.Graph.Internal;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph
{
    /// <summary>Describe the type of a <see cref="TaskError"/></summary>
    public enum TaskErrorType
    {
        Undefined, // Prevent implicit initialization of underlying value 0 to a valid value (UnhandledException)
        
        /// <summary>
        /// Inform about an unhandled exception in a <see cref="EntityContainer"/> implementation which need to be fixed.
        /// More information at <see cref="EntityDatabase.ExecuteSync"/>.
        /// Maps to <see cref="TaskErrorResultType.UnhandledException"/>.
        /// </summary>
        UnhandledException,
        
        /// <summary>
        /// Inform about an error when accessing a database.
        /// E.g. the access is currently not available or accessing a missing table.
        /// maps to <see cref="TaskErrorResultType.DatabaseError"/>
        /// </summary>
        DatabaseError,
        
        InvalidTask,
        
        PermissionDenied,
        
        SyncError,
        
        /// <summary>
        /// It is set for a <see cref="SyncTask"/> if a <see cref="SyncResponse"/> contains errors in its
        /// <see cref="Dictionary{K,V}"/> fields containing <see cref="EntityErrors"/> for entities accessed via a CRUD
        /// command by the <see cref="SyncTask"/>.
        /// The entity errors are available via <see cref="TaskError.entityErrors"/>.  
        /// No mapping to a <see cref="TaskErrorResultType"/> value.
        /// </summary>
        EntityErrors
    }
    
    public class TaskError {
        public   readonly   TaskErrorType                       type;
        /// The entities caused that task failed. Return empty dictionary in case of no entity errors. Is never null.
        public   readonly   IDictionary<JsonKey, EntityError>   entityErrors;
        /// Return a single line error message. Is never null.
        public   readonly   string                              taskMessage;
        /// Return the stacktrace for an <see cref="TaskErrorType.UnhandledException"/> if provided. Otherwise null.
        private  readonly   string                              stacktrace;
        /// Note: Does not contain <see cref="stacktrace"/> in any case to avoid leaking internals when using <see cref="Message"/>
        public              string                              Message     => GetMessage(false);
        public   override   string                              ToString()  => GetMessage(true);
       
        private static readonly IDictionary<JsonKey, EntityError> NoErrors = new EmptyDictionary<JsonKey, EntityError>();

        internal TaskError(TaskErrorResult error) {
            type                = TaskToSyncError(error.type);
            taskMessage         = error.message;
            stacktrace          = error.stacktrace;
            entityErrors        = NoErrors;
        }

        internal TaskError(IDictionary<JsonKey, EntityError> entityErrors) {
            this.entityErrors   = entityErrors ?? throw new ArgumentException("entityErrors must not be null");
            type                = TaskErrorType.EntityErrors;
            taskMessage         = "EntityErrors";
        }
        
        private static TaskErrorType TaskToSyncError(TaskErrorResultType type) {
            switch (type) {
                case TaskErrorResultType.UnhandledException:    return TaskErrorType.UnhandledException;
                case TaskErrorResultType.DatabaseError:         return TaskErrorType.DatabaseError;
                case TaskErrorResultType.InvalidTask:           return TaskErrorType.InvalidTask;
                case TaskErrorResultType.PermissionDenied:      return TaskErrorType.PermissionDenied;
                case TaskErrorResultType.SyncError:             return TaskErrorType.SyncError;
            }
            throw new ArgumentException($"cant convert error type: {type}");
        }
        
        /// Note: The library itself set <param name="showStack"/> to true only when called from <see cref="object.ToString"/>
        public string GetMessage(bool showStack) {
            var sb = new StringBuilder();
            AppendAsText("| ", sb, 10, showStack);
            return sb.ToString();
        }

        internal void AppendAsText(string prefix, StringBuilder sb, int maxEntityErrors, bool showStack) {
            if (type != TaskErrorType.EntityErrors) {
                sb.Append(type);
                sb.Append(" ~ ");
                sb.Append(taskMessage);
                if (showStack && stacktrace != null) {
                    sb.Append("\n");
                    sb.Append(stacktrace);
                }
                return;
            }
            var errors = entityErrors;
            sb.Append("EntityErrors ~ count: ");
            sb.Append(errors.Count);
            int n = 0;
            foreach (var errorPair in errors) {
                var error = errorPair.Value;
                sb.Append('\n');
                if (n++ == maxEntityErrors) {
                    sb.Append(prefix);
                    sb.Append("...");
                    break;
                }
                error.AppendAsText(prefix, sb, showStack);
            }
        }
    }
}