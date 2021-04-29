// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Flow.Graph
{
    // ----------------------------------------- CreateTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class CreateTask<T> where T : Entity
    {
        private readonly    T           entity;

        internal            T           Entity      => entity;
        public   override   string      ToString()  => entity.id;
        
        internal CreateTask(T entity) {
            this.entity = entity;
        }

        // public T Result  => entity;
    }
}