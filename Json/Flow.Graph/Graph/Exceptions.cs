// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Database.Models;

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
    
    public class TaskEntityException : Exception
    {
        public readonly  List<EntityError> errors;

        public TaskEntityException(List<EntityError> errors) : base(GetMessage(errors)) {
            this.errors = errors;
        }

        private static string GetMessage(List<EntityError> errors) {
            var sb = new StringBuilder();
            sb.Append("Task failed by entity errors. Count: ");
            sb.Append(errors.Count);
            int n = 0;
            foreach (var error in errors) {
                if (n++ == 10) {
                    sb.Append("\n...");
                    break;
                }
                sb.Append("\n| ");
                sb.Append(error.GetMessage());
            }
            return sb.ToString();
        }
    }
}