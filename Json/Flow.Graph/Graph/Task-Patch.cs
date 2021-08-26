// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph
{
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PatchTask<T> : SyncTask where T : class
    {
        internal readonly   List<string>        members = new List<string>();
        internal readonly   List<Peer<T>>       peers   = new List<Peer<T>>();
        private  readonly   EntityPeerSet<T>    set;

        internal            TaskState           state;
        internal override   TaskState           State      => state;
        
        public   override   string              Details {
            get {
                var sb = new StringBuilder();
                sb.Append("PatchTask<");
                sb.Append(typeof(T).Name);
                sb.Append("> #ids: ");
                sb.Append(peers.Count);
                sb.Append(", members: [");
                for (int n = 0; n < members.Count; n++) {
                    if (n > 0)
                        sb.Append(", ");
                    sb.Append(members[n]);
                }
                sb.Append("]");
                return sb.ToString();
            }
        }
        

        internal PatchTask(Peer<T> peer, EntityPeerSet<T> set) {
            this.set = set;
            peers.Add(peer);
        }
        
        internal PatchTask(ICollection<Peer<T>> peers, EntityPeerSet<T> set) {
            this.set = set;
            this.peers.AddRange(peers);
        }

        public void Add(T entity) {
            var peer = set.GetPeerByEntity(entity);
            peers.Add(peer);
        }
        
        public void AddRange(ICollection<T> entities) {
            var newPeers = new List<Peer<T>>(entities.Count);
            foreach (var entity in entities) {
                var peer = set.GetPeerByEntity(entity);
                newPeers.Add(peer);
            }
            peers.AddRange(newPeers);
        }
        
        public void Member(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentException($"PatchTask<{typeof(T).Name}>.Member() member must not be null.");
            var memberPath = Operation.PathFromLambda(member, EntitySet.RefQueryPath);
            members.Add(memberPath);
        }
        
        public void MemberPath(MemberPath<T> member) {
            if (member == null)
                throw new ArgumentException($"PatchTask<{typeof(T).Name}>.MemberPath() member must not be null.");
            members.Add(member.path);
        }
        
        public void MemberPaths(ICollection<MemberPath<T>> members) {
            if (members == null)
                throw new ArgumentException($"PatchTask<{typeof(T).Name}>.MemberPaths() members must not be null.");
            int n = 0;
            foreach (var member in members) {
                if (member == null)
                    throw new ArgumentException($"PatchTask<{typeof(T).Name}>.MemberPaths() members[{n}] must not be null.");
                n++;
                this.members.Add(member.path);
            }
        }
    }
    
    public class MemberPath<T>
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