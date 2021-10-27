// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Client
{
    public sealed class UnresolvedRefException : Exception
    {
        public readonly     string          key;
        
        internal UnresolvedRefException(string message, Type type, string key)
            : base ($"{message} Ref<{type.Name}> (Key: '{key}')")
        {
            this.key = key;
        }
    }
    
    public sealed class TaskNotSyncedException : Exception
    {
        internal TaskNotSyncedException(string message) : base (message) { }
    }
    
    public sealed class TaskAlreadySyncedException : Exception
    {
        internal TaskAlreadySyncedException(string message) : base (message) { }
    }
    
    public sealed class TaskResultException : Exception
    {
        public readonly     TaskError       error;
        
        internal TaskResultException(TaskError error) : base(error.GetMessage(false)) {
            this.error      = error;
        }
    }

    public sealed class SyncTasksException : Exception
    {
        public readonly     List<SyncTask>  failed;

        internal SyncTasksException(ErrorResponse errorResponse, List<SyncTask> failed)
            : base(SyncResult.GetMessage(errorResponse, failed))
        {
            this.failed = failed;
        }
    }
}