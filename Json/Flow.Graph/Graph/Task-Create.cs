// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
    public abstract class WriteTask : SyncTask {
        internal            TaskState   state;
        internal override   TaskState   State      => state;

        internal abstract void GetIds(List<string> ids);
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class CreateTask<T> : WriteTask where T : Entity
    {
        private readonly    ICollection<T>  entities;

        internal override   string          Label       => $"CreateTask<{typeof(T).Name}> #ids: {entities.Count}";
        public   override   string          ToString()  => Label;
        
        internal CreateTask(ICollection<T> entities) {
            this.entities = entities;
        }

        internal override void GetIds(List<string> ids) {
            foreach (var entity in entities) {
                ids.Add(entity.id);    
            }
        }
    }
}