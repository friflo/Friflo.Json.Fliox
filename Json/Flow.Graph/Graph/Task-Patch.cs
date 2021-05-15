// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph
{
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PatchTask<T> : SyncTask where T : Entity
    {
        internal readonly   List<string>                paths = new List<string>();
        internal readonly   ICollection<PeerEntity<T>>  peers;

        internal            TaskState                   state;
        internal override   TaskState                   State      => state;
        
        internal override   string      Label       => $"PatchTask<{typeof(T).Name}> #ids: {peers.Count}";
        public   override   string      ToString()  => Label;
        
        internal PatchTask(ICollection<PeerEntity<T>> peers) {
            this.peers  = peers;
        }
        
        public void Member(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentException($"PatchTask<{typeof(T).Name}>.Member() member must not be null.");
            var memberPath = Operation.PathFromLambda(member, EntitySet.RefQueryPath);
            paths.Add(memberPath);
        }
        
        public void MemberPath(MemberPath<T> member) {
            if (member == null)
                throw new ArgumentException($"PatchTask<{typeof(T).Name}>.Member() member must not be null.");
            paths.Add(member.path);
        }
    }
    
    public class MemberPath<T>
    {
        internal readonly string path;

        public MemberPath(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentException($"MemberPath<{typeof(T).Name}>() member must not be null.");
            path = Operation.PathFromLambda(member, EntitySet.RefQueryPath);
        }
    }

}