// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Client.Internal;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    // ----------------------------------------- ReadRefTask<T> -----------------------------------------
    [CLSCompliant(true)]
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
        
        internal override void SetResult(Entity[] entities) {
            if (entities.Length == 0) {
                return;
            }
            if (entities.Length != 1) {
                throw new InvalidOperationException($"Expect values with one element. got: {entities.Length}, task: {this}");
            }
            var entity0    = entities[0];
            if (entity0.error == null) {
                entity  = (T)entity0.value;
            } else {
                var entityErrorInfo = new TaskErrorInfo();
                entityErrorInfo.AddEntityError(entity0.error);
                state.SetError(entityErrorInfo);
            }
        }
    }
}