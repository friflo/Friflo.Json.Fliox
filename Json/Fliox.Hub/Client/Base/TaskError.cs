// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /*
    /// <summary>Describe the type of a <see cref="TaskError"/></summary>
    public enum TaskErrorType2
    {
        Undefined           = 0, // Prevent implicit initialization of underlying value 0 to a valid value (UnhandledException)
        
        /// <summary>
        /// Inform about an unhandled exception in a <see cref="EntityContainer"/> implementation which need to be fixed.
        /// More information at <see cref="FlioxHub.ExecuteRequestAsync"/>.
        /// Maps to <see cref="TaskErrorType.UnhandledException"/>.
        /// </summary>
        UnhandledException  = 1,
        
        /// <summary>A general database error.</summary>
        /// <remarks>
        /// E.g. the access is currently not available or accessing a missing table.
        /// maps to <see cref="TaskErrorType.DatabaseError"/>
        /// </remarks>
        DatabaseError       = 2,
        /// <summary>Invalid query filter</summary>
        FilterError         = 3,
        /// <summary>Schema validation of an entity failed</summary>
        ValidationError     = 4,
        /// <summary>Execution of message / command failed caused by invalid input</summary>
        CommandError        = 5,
        /// <summary>database message / command not implemented</summary>
        NotImplemented      = 6,
        /// <summary>Invalid task. E.g. by using an invalid task parameter</summary>
        InvalidTask         = 7,
        /// <summary> Task execution denied </summary>
        PermissionDenied    = 8,
        /// <summary>The entire <see cref="SyncRequest"/> containing a task failed</summary>
        SyncError           = 9,
        /// <summary>
        /// It is set for a <see cref="SyncTask"/> if a <see cref="SyncResponse"/> contains errors in its
        /// <see cref="Dictionary{K,V}"/> fields containing <see cref="EntityErrors"/> for entities accessed via a CRUD
        /// command by the <see cref="SyncTask"/>.
        /// The entity errors are available via <see cref="TaskError.entityErrors"/>.  
        /// No mapping to a <see cref="TaskErrorType"/> value.
        /// </summary>
        EntityErrors        = 10,
        /// <summary> Use to indicate an invalid response.</summary>
        /// <remarks>No mapping to a <see cref="TaskErrorType"/> value.</remarks>
        InvalidResponse     = 11,
    } */
    
    /// <summary>
    /// Contains the <see cref="type"/> and the error <see cref="Message"/> of task in case its execution failed.
    /// </summary>
    public sealed class TaskError {
        public   readonly   TaskErrorType                       type;
        /// The entities caused that task failed. Return empty dictionary in case of no entity errors. Is never null.
        public   readonly   IDictionary<JsonKey, EntityError>   entityErrors;
        /// Return a single line error message. Is never null.
        public   readonly   string                              taskMessage;
        /// Return the stacktrace for an <see cref="TaskErrorType.UnhandledException"/> if provided. Otherwise null.
        private  readonly   string                              stacktrace;
        /// Note: Does contain an exception <see cref="stacktrace"/> in case the used <see cref="FlioxHub"/> provide a stacktrace.
        public              string                              Message     => GetMessage(true);
        public   override   string                              ToString()  => GetMessage(true);
       
        private static readonly IDictionary<JsonKey, EntityError> NoErrors = new EmptyDictionary<JsonKey, EntityError>();

        internal TaskError(TaskErrorResult error) {
            type                = error.type;
            taskMessage         = error.message;
            stacktrace          = error.stacktrace;
            entityErrors        = NoErrors;
        }
        
        internal TaskError(TaskErrorType type, string message) {
            this.type           = type;
            taskMessage         = message;
            stacktrace          = null;
            entityErrors        = NoErrors;
        }


        internal TaskError(IDictionary<JsonKey, EntityError> entityErrors) {
            this.entityErrors   = entityErrors ?? throw new ArgumentException("entityErrors must not be null");
            type                = TaskErrorType.EntityErrors;
            taskMessage         = "EntityErrors";
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
                    sb.Append('\n');
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