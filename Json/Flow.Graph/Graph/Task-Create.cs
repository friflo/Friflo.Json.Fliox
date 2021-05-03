// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
    public abstract class CreateTask : SyncTask {
        internal            TaskState   state;
        internal override   TaskState   State      => state;
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class CreateTask<T> : CreateTask where T : Entity
    {
        private readonly    T           entity;

        internal override   string      Label       => $"CreateTask<{typeof(T).Name}> id: {entity.id}";
        public   override   string      ToString()  => Label;
        
        internal CreateTask(T entity) {
            this.entity = entity;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class CreateRangeTask<T> : CreateTask where T : Entity
    {
        private  readonly   ICollection<T>  entities;

        internal override   string          Label       => $"CreateRangeTask<{typeof(T).Name}> #ids: {entities.Count}";
        public   override   string          ToString()  => Label;
        
        internal CreateRangeTask(ICollection<T> entities) {
            this.entities = entities;
        }
    }
}