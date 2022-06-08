// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class PatchTask<TKey, T> : SyncTask where T : class
    {
        internal readonly   List<string>        members;
        internal readonly   List<EntityPatch>   patches = new List<EntityPatch>();
        private  readonly   SyncSet<TKey,T>     syncSet;

        internal            TaskState           state;
        internal override   TaskState           State      => state;
        
        public   override   string              Details {
            get {
                var sb = new StringBuilder();
                sb.Append("PatchTask<");
                sb.Append(typeof(T).Name);
                sb.Append("> patches: ");
                sb.Append(patches.Count);
                sb.Append(", members: [");
                for (int n = 0; n < members.Count; n++) {
                    if (n > 0)
                        sb.Append(", ");
                    sb.Append(members[n]);
                }
                sb.Append(']');
                return sb.ToString();
            }
        }

        internal PatchTask(SyncSet<TKey,T> syncSet, PatchMember<T> patchMember) {
            this.syncSet    = syncSet;
            members         = patchMember.members;
        }

        public PatchTask<TKey, T> Add(T entity) {
            using (var pooled = syncSet.ObjectMapper.Get()) {
                var entities = new List<T> { entity };
                syncSet.CreatePatch(this, entities, pooled.instance);
            }
            return this;
        }
        
        public PatchTask<TKey, T> AddRange(ICollection<T> entities) {
            using (var pooled = syncSet.ObjectMapper.Get()) {
                syncSet.CreatePatch(this, entities, pooled.instance);
            }
            return this;
        }
    }
    
    public class PatchMember<T> where T : class
    {
        internal readonly   List<string>        members = new List<string>();
        
        public void Add(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            var memberPath = Operation.PathFromLambda(member, EntitySet.RefQueryPath);
            members.Add(memberPath);
        }
        public void Add(MemberPath<T> memberPath) {
            if (memberPath == null)
                throw new ArgumentNullException(nameof(memberPath));
            members.Add(memberPath.path);
        }
    }
    
    public sealed class MemberPath<T>
    {
        internal readonly string path;

        public override string ToString() => path;

        public MemberPath(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentException($"MemberPath<{typeof(T).Name}>() member must not be null.");
            path = Operation.PathFromLambda(member, EntitySet.RefQueryPath);
        }
    }

}