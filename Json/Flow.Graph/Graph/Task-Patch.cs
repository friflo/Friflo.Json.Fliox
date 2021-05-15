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
        protected readonly          List<JsonPatch> patches;
        
        protected static readonly   QueryPath       RefQueryPath = new RefQueryPath();

        protected PatchTask(List<JsonPatch> patches) {
            this.patches = patches;
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
        
        internal PatchTask(string id, List<JsonPatch> patches) : base(patches) {
            this.id = id;
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
        private  readonly   ICollection<string>  ids;

        internal override   string          Label       => $"PatchRangeTask<{typeof(T).Name}> #ids: {ids.Count}";
        public   override   string          ToString()  => Label;
        
        internal PatchRangeTask(ICollection<string> ids, List<JsonPatch> patches) : base(patches) {
            this.ids = ids;
        }
        
        internal override void GetIds(List<string> ids) {
            foreach (var id in ids) {
                ids.Add(id);    
            }
        }
    }

}