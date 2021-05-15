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
    public abstract class PatchTask : SyncTask {
        internal            TaskState   state;
        internal override   TaskState   State      => state;

        protected static readonly   QueryPath       RefQueryPath = new RefQueryPath();
        
        internal abstract void GetPeers(List<PeerEntity> ids);
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PatchTask<T> : PatchTask where T : Entity
    {
        private readonly    PeerEntity<T>   peer;
        private readonly    List<JsonPatch> patches;


        internal override   string      Label       => $"PatchTask<{typeof(T).Name}> id: {peer.entity.id}";
        public   override   string      ToString()  => Label;
        
        internal PatchTask(PeerEntity<T> peer, List<JsonPatch> patches) {
            this.peer    = peer;
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

        internal override void GetPeers(List<PeerEntity> peers) {
            peers.Add(peer);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PatchRangeTask<T> : PatchTask where T : Entity
    {
        private  readonly   ICollection<PeerEntity<T>>  peers;
        private readonly    EntitySet<T>                set;

        internal override   string          Label       => $"PatchRangeTask<{typeof(T).Name}> #ids: {peers.Count}";
        public   override   string          ToString()  => Label;
        
        internal PatchRangeTask(ICollection<PeerEntity<T>>  peers, EntitySet<T> set) {
            this.peers  = peers;
            this.set    = set;
        }
        
        public void Member(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentException($"PatchRangeTask<{typeof(T).Name}>.Member() member must not be null.");
            var memberPath = Operation.PathFromLambda(member, RefQueryPath);
            set.sync.AddPatches(peers, memberPath);
        }
        
        internal override void GetPeers(List<PeerEntity> peers) {
            foreach (var peer in this.peers) {
                peers.Add(peer);    
            }
        }
    }

}