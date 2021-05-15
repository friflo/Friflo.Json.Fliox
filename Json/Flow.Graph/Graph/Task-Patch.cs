// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Flow.Graph
{
    public abstract class PatchTask : WriteTask {
        private readonly    string           path;

        protected PatchTask(string path) {
            this.path = path;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PatchTask<T> : PatchTask where T : Entity
    {
        private readonly    string      id;

        internal override   string      Label       => $"PatchTask<{typeof(T).Name}> id: {id}";
        public   override   string      ToString()  => Label;
        
        internal PatchTask(string id, string path) : base(path) {
            this.id = id;
        }
        
        internal override void GetIds(List<string> ids) {
            ids.Add(id);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PatchRangeTask<T> : PatchTask where T : Entity
    {
        private  readonly   ICollection<string>  ids;

        internal override   string          Label       => $"PatchRangeTask<{typeof(T).Name}> #ids: {ids.Count}";
        public   override   string          ToString()  => Label;
        
        internal PatchRangeTask(ICollection<string> ids, string path) : base(path) {
            this.ids = ids;
        }
        
        internal override void GetIds(List<string> ids) {
            foreach (var id in ids) {
                ids.Add(id);    
            }
        }
    }

}