// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Flow.Graph
{
    // ----------------------------------------- CreateTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class CreateTask<T> where T : Entity
    {
        private readonly    T           entity;

        private             string      Label       => $"CreateTask<{typeof(T).Name}> id: {entity.id}";
        public   override   string      ToString()  => Label;
        
        internal CreateTask(T entity) {
            this.entity = entity;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class CreateRangeTask<T> where T : Entity
    {
        private  readonly   ICollection<T>  entities;

        private             string          Label       => $"CreateRangeTask<{typeof(T).Name}> #ids: {entities.Count}";
        public   override   string          ToString()  => Label;
        
        internal CreateRangeTask(ICollection<T> entities) {
            this.entities = entities;
        }
    }
}