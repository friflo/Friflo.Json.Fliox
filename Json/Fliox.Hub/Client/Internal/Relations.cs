// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal struct Relations
    {
        internal readonly   IRelationsParent    parent;
        internal            SubRelations        subRelations;

        internal Relations(IRelationsParent parent) {
            this.parent         = parent ?? throw new InvalidOperationException("Expect task not null");

            this.subRelations   = new SubRelations();
        }

        internal ReadRelations<TRef> ReadRelationsByExpression<TRef>(Set relation, Expression expression, FlioxClient client, IRelationsParent parent) where TRef : class {
            string path = ExpressionSelector.PathFromExpression(expression, out _);
            return ReadRelationsByPath<TRef>(relation, path, client, parent);
        }
        
        internal ReadRelations<TRef> ReadRelationsByPath<TRef>(Set relation, string selector, FlioxClient client, IRelationsParent parent) where TRef : class {
            if (subRelations.TryGetTask(selector, out ReadRelationsFunction readRelationsFunction))
                return (ReadRelations<TRef>)readRelationsFunction;
            // var relation = store._intern.GetSetByType(typeof(TValue));
            var readRelations   = new ReadRelations<TRef>(parent, selector, relation, client);
            subRelations.AddReadRelations(selector, readRelations);
            // client.AddFunction(readRelations);
            return readRelations;
        }
    }
    
    internal struct SubRelations // : IEnumerable <BinaryPair>   <- not implemented to avoid boxing
    {
        /// key: <see cref="ReadRelationsFunction.Selector"/>
        private     Dictionary<string, ReadRelationsFunction>   map; // map == null if no tasks added
        
        internal    int                                         Count => map?.Count ?? 0;
        internal    ReadRelationsFunction                       this[string key] => map[key];
        
        internal bool TryGetTask(string selector, out ReadRelationsFunction subRelationsTask) {
            if (map == null) {
                subRelationsTask = null;
                return false;
            }
            return map.TryGetValue(selector, out subRelationsTask);
        }
        
        internal void AddReadRelations(string selector, ReadRelationsFunction readRelations) {
            if (map == null) {
                map = new Dictionary<string, ReadRelationsFunction>();
            }
            map.Add(selector, readRelations);
        }

        // return ValueIterator instead of IEnumerator<ReadRefsTask> to avoid boxing 
        public ValueIterator<string, ReadRelationsFunction> GetEnumerator() {
            return new ValueIterator<string, ReadRelationsFunction>(map);
        }
    }
}