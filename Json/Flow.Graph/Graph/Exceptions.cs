// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Graph.Internal;
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
        public readonly  TaskErrorInfo error;

        internal TaskResultException(TaskErrorInfo taskErrorInfo) : base(GetMessage(taskErrorInfo)) {
            this.error = taskErrorInfo;
        }

        private static string GetMessage(TaskErrorInfo taskErrorInfo) {
            var errors = taskErrorInfo.EntityErrors;
            var sb = new StringBuilder();
            sb.Append("Task failed by entity errors. Count: ");
            sb.Append(errors.Count);
            int n = 0;
            foreach (var errorPair in errors) {
                var error = errorPair.Value;
                if (n++ == 10) {
                    sb.Append("\n...");
                    break;
                }
                sb.Append("\n| ");
                error.AppendAsText(sb);
            }
            return sb.ToString();
        }
    }
}