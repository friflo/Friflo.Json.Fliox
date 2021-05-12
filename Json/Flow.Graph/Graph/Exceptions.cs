// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    public class UnresolvedRefException : Exception
    {
        public readonly Entity entity;
        
        public UnresolvedRefException(string message, Entity entity)
            : base ($"{message} Ref<{entity.GetType().Name}> id: {entity.id}")
        {
            this.entity = entity;
        }
    }
    
    public class TaskNotSyncedException : Exception
    {
        public TaskNotSyncedException(string message) : base (message) { }
    }
    
    public class TaskAlreadySyncedException : Exception
    {
        public TaskAlreadySyncedException(string message) : base (message) { }
    }
    
    public class TaskResultException : Exception
    {
        public readonly     TaskErrorType                       taskError;
        public readonly     IDictionary<string, EntityError>    entityErrors;
        
        internal TaskResultException(TaskError error) : base(error.GetMessage()) {
            taskError       = error.type;
            entityErrors    = error.entityErrors;
        }
    }

    public class SyncResultException : Exception
    {
        public readonly List<SyncTask> failed;

        internal SyncResultException(List<SyncTask> failed) : base(SyncResult.GetMessage(failed)) {
            this.failed = failed;
        }
    }
}