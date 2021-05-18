// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
    public class RefsPath<TEntity, TRef>    where TEntity : Entity
                                            where TRef    : Entity 
    {
        public readonly string path;

        public override string ToString() => path;

        internal RefsPath(string path) {
            this.path = path;
        }
        
        public static RefsPath<TEntity, TRef> MemberRefs(Expression<Func<TEntity, IEnumerable<Ref<TRef>>>> selector)  {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RefsPath<TEntity, TRef>(selectorPath);
        }
    }
    
    public class RefPath<TEntity, TRef> : RefsPath<TEntity, TRef>
                                            where TEntity : Entity
                                            where TRef    : Entity 
    {
        internal RefPath(string path) : base (path) { }
        
        public static RefPath<TEntity, TRef> MemberRef(Expression<Func<TEntity, Ref<TRef>>> selector) {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RefPath<TEntity, TRef>(selectorPath);
        }
    }
}