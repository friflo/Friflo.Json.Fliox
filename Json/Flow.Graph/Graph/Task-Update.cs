// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Flow.Graph
{
    public abstract class UpdateTask : WriteTask { }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class UpdateTask<T> : UpdateTask where T : Entity
    {
        private readonly    T           entity;

        internal override   string      Label       => $"UpdateTask<{typeof(T).Name}> id: {entity.id}";
        public   override   string      ToString()  => Label;
        
        internal UpdateTask(T entity) {
            this.entity = entity;
        }
        
        internal override void GetIds(List<string> ids) {
            ids.Add(entity.id);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class UpdateRangeTask<T> : UpdateTask where T : Entity
    {
        private  readonly   ICollection<T>  entities;

        internal override   string          Label       => $"UpdateRangeTask<{typeof(T).Name}> #ids: {entities.Count}";
        public   override   string          ToString()  => Label;
        
        internal UpdateRangeTask(ICollection<T> entities) {
            this.entities = entities;
        }
        
        internal override void GetIds(List<string> ids) {
            foreach (var entity in entities) {
                ids.Add(entity.id);    
            }
        }
    }

}