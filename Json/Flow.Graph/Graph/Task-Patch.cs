// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Mapper.Map.Val;
using Friflo.Json.Flow.Transform;
using Friflo.Json.Flow.Transform.Query;

namespace Friflo.Json.Flow.Graph
{
    public abstract class PatchTask : WriteTask {
        protected static readonly   QueryPath       RefQueryPath = new RefQueryPath();
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PatchTask<T> : PatchTask where T : Entity
    {
        private readonly    string          id;
        private readonly    List<JsonPatch> patches;


        internal override   string      Label       => $"PatchTask<{typeof(T).Name}> id: {id}";
        public   override   string      ToString()  => Label;
        
        internal PatchTask(string id, List<JsonPatch> patches) {
            this.id      = id;
            this.patches = patches;
        }
        
        public void Member(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentException($"PatchTask<{typeof(T).Name}>.Member() member must not be null.");
            var memberPath = Operation.PathFromLambda(member, RefQueryPath);
            var value = new JsonValue {
                json = "null" // todo get current member value as JSON
            };
            patches.Add(new PatchReplace {
                path = memberPath,
                value = value
            });
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
        private  readonly   ICollection<string> ids;
        private readonly    EntitySet<T>        set;

        internal override   string          Label       => $"PatchRangeTask<{typeof(T).Name}> #ids: {ids.Count}";
        public   override   string          ToString()  => Label;
        
        internal PatchRangeTask(ICollection<string> ids, EntitySet<T> set) {
            this.ids = ids;
            this.set = set;
        }
        
        public void Member(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentException($"PatchRangeTask<{typeof(T).Name}>.Member() member must not be null.");
            var memberPath = Operation.PathFromLambda(member, RefQueryPath);
            set.sync.AddPatches(ids, memberPath);
        }
        
        internal override void GetIds(List<string> ids) {
            foreach (var id in ids) {
                ids.Add(id);    
            }
        }
    }

}