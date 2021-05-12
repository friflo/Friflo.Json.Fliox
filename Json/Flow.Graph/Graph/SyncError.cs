// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    public enum SyncErrorType
    {
        Undefined, // Prevent implicit initialization of underlying value 0 to a valid value (UnhandledException) 
        UnhandledException,
        DatabaseError,
        EntityErrors // Is set only by Flow.Graph implementation - not by Flow.Database
    }
    
    // todo rename to TaskError
    public class SyncError {
        public   readonly   SyncErrorType                       type;
        public   readonly   string                              message;
        public   readonly   IDictionary<string, EntityError>    entityErrors;
       
        private static readonly IDictionary<string, EntityError> NoErrors = new EmptyDictionary<string, EntityError>();

        internal SyncError(TaskErrorResult error) {
            type            = TaskToSyncError(error.type);
            message         = error.message;
            entityErrors    = NoErrors;
        }

        internal SyncError(IDictionary<string, EntityError> entityErrors) {
            this.entityErrors   = entityErrors ?? throw new ArgumentException("entityErrors must not be null");
            type                = SyncErrorType.EntityErrors;
            message             = "Task failed by entity errors";
        }
        
        private static SyncErrorType TaskToSyncError(TaskErrorResultType type) {
            switch (type) {
                case TaskErrorResultType.UnhandledException:  return SyncErrorType.UnhandledException;
                case TaskErrorResultType.DatabaseError:       return SyncErrorType.DatabaseError;
            }
            throw new ArgumentException($"cant convert error type: {type}");
        }
        
        public   override   string                              ToString() {
            if (type == SyncErrorType.EntityErrors) {
                return $"type: {type}, message: {message}, entityErrors: {entityErrors.Count}";
            }
            return $"type: {type}, message: {message}";
        }
    }
}