// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class UnresolvedRefException : Exception
    {
        public readonly Entity entity;
        
        public UnresolvedRefException(string message, Entity entity)
            : base ($"{message} Ref<{entity.GetType().Name}> id: {entity.id}")
        {
            this.entity = entity;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class TaskNotSyncedException : Exception
    {
        public TaskNotSyncedException(string message) : base (message) { }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
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

        internal SyncResultException(List<SyncTask> failed) : base(GetMessage(failed))
        {
            this.failed = failed;
        }
        
        private static string GetMessage(List<SyncTask> failed) {
            var sb = new StringBuilder();
            sb.Append("Sync() failed with task errors. Count: ");
            sb.Append(failed.Count);
            foreach (var task in failed) {
                sb.Append("\n| ");
                sb.Append(task.Label); // todo should use appender instead of Label
                sb.Append(" - ");
                var error = task.GetTaskError();
                error.AppendAsText("| ", sb, 3);
            }
            return sb.ToString();
        }
    }
}