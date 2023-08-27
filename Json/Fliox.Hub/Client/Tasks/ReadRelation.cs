// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Models;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    // ----------------------------------------- ReadRefTask<T> -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class ReadRelation<T> : ReadRelationsFunction<T> where T : class
    {
        private             T                   entity;
        private   readonly  IRelationsParent    parent;
    
        public              T           Result  => IsOk("ReadRelation.Result", out Exception e) ? entity  : throw e;
                
        // internal  override  TaskState   State   => state;
        public    override  string      Label   => $"{parent.Label} -> {Selector}";
        public    override  string      Details => $"{parent.Details} -> {Selector}";
                
        internal  override  string      Selector    { get; }
        internal  override  Set         Relation   { get; }
        public    override  SyncTask    Task        => parent.Task;

        internal override   SubRelations SubRelations => relations.subRelations;

        internal ReadRelation(IRelationsParent parent, string selector, Set relation, FlioxClient client)
            : base(client, parent)
        {
            relations       = new Relations(parent);
            this.parent     = parent;
            this.Selector   = selector;
            this.Relation   = relation;
        }
        
        internal override void SetResult(Set set, EntityValue[] values) {
            if (values.Length == 0) {
                return;
            }
            if (values.Length != 1)
                throw new InvalidOperationException($"Expect values with one element. got: {values.Length}, task: {this}");
            var entitySet = (Set<T>) set;
            var id      = values[0].key;
            var peer    = entitySet.GetPeerById(id);
            if (peer.error == null) {
                entity  = peer.Entity;
            } else {
                var entityErrorInfo = new TaskErrorInfo();
                entityErrorInfo.AddEntityError(peer.error);
                state.SetError(entityErrorInfo);
            }
        }
    }
}