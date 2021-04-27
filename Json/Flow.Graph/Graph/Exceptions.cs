// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

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
}