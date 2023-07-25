// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal;

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
        internal  override  ShortString Container   { get; }
        internal  override  string      KeyName     { get; }
        internal  override  bool        IsIntKey    { get; }
        public    override  SyncTask    Task        => parent.Task;

        internal override   SubRelations SubRelations => relations.subRelations;

        internal ReadRelation(IRelationsParent parent, string selector, in ShortString container, string keyName, bool isIntKey, FlioxClient client)
            : base(client, parent)
        {
            relations       = new Relations(parent);
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
            this.KeyName    = keyName;
            this.IsIntKey   = isIntKey;
        }
        
        internal override void SetResult(EntitySet set, List<JsonKey> ids) {
            if (ids.Count == 0) {
                return;
            }
            if (ids.Count != 1)
                throw new InvalidOperationException($"Expect ids result set with one element. got: {ids.Count}, task: {this}");
            var entitySet = (EntitySetBase<T>) set;
            var id      = ids[0];
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