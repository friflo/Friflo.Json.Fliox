// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Client
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
    
    public sealed class TaskNotSendException : Exception
    {
        internal TaskNotSendException(string message) : base (message) { }
    }
    
    public sealed class TaskAlreadySendException : Exception
    {
        internal TaskAlreadySendException(string message) : base (message) { }
    }
    
    public sealed class TaskResultException : Exception
    {
        public readonly     TaskError       error;
        
        internal TaskResultException(TaskError error) : base(error.GetMessage(false)) {
            this.error      = error;
        }
    }

    public sealed class ExecuteTasksException : Exception
    {
        public readonly     List<SyncTask>  failed;

        internal ExecuteTasksException(ErrorResponse errorResponse, List<SyncTask> failed) : base(ExecuteTasksResult.GetMessage(errorResponse, failed)) {
            this.failed = failed;
        }
    }
}