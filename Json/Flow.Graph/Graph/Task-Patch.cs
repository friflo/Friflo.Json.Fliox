// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Transform;
using Friflo.Json.Flow.Transform.Query;

namespace Friflo.Json.Flow.Graph
{
    public abstract class PatchTask : SyncTask
    {
        internal readonly   List<string>    paths = new List<string>();
        internal            TaskState       state;
        internal override   TaskState       State      => state;

        internal static readonly   QueryPath       RefQueryPath = new RefQueryPath();
        
        internal abstract void GetPeers(List<PeerEntity> peerList);
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PatchTask<T> : PatchTask where T : Entity
    {
        private readonly    PeerEntity<T>   peer;

        internal override   string      Label       => $"PatchTask<{typeof(T).Name}> id: {peer.entity.id}";
        public   override   string      ToString()  => Label;
        
        internal PatchTask(PeerEntity<T> peer) {
            this.peer    = peer;
        }
        
        public void Member(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentException($"PatchTask<{typeof(T).Name}>.Member() member must not be null.");
            var memberPath = Operation.PathFromLambda(member, RefQueryPath);
            paths.Add(memberPath);
        }
        
        public void MemberPath(MemberPath<T> member) {
            if (member == null)
                throw new ArgumentException($"PatchTask<{typeof(T).Name}>.Member() member must not be null.");
            paths.Add(member.path);
        }

        internal override void GetPeers(List<PeerEntity> peerList) {
            peerList.Add(peer);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PatchRangeTask<T> : PatchTask where T : Entity
    {
        private  readonly   ICollection<PeerEntity<T>>  peers;

        internal override   string      Label       => $"PatchRangeTask<{typeof(T).Name}> #ids: {peers.Count}";
        public   override   string      ToString()  => Label;
        
        internal PatchRangeTask(ICollection<PeerEntity<T>> peers) {
            this.peers  = peers;
        }
        
        public void Member(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentException($"PatchRangeTask<{typeof(T).Name}>.Member() member must not be null.");
            var memberPath = Operation.PathFromLambda(member, RefQueryPath);
            paths.Add(memberPath);
        }
        
        public void MemberPath(MemberPath<T> member) {
            if (member == null)
                throw new ArgumentException($"PatchRangeTask<{typeof(T).Name}>.Member() member must not be null.");
            paths.Add(member.path);
        }
        
        internal override void GetPeers(List<PeerEntity> peerList) {
            foreach (var peer in peers) {
                peerList.Add(peer);    
            }
        }
    }
    
    public class MemberPath<T>
    {
        internal readonly string path;

        public MemberPath(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentException($"MemberPath<{typeof(T).Name}>() member must not be null.");
            path = Operation.PathFromLambda(member, PatchTask.RefQueryPath);
        }
    }

}