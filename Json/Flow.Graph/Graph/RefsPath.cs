// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
    public class RefsPath<TKey, TEntity, TRef>  where TEntity : class
                                                where TRef    : class 
    {
        public readonly string path;

        public override string ToString() => path;

        internal RefsPath(string path) {
            this.path = path;
        }
        
        public static RefsPath<TKey, TEntity, TRef> MemberRefs(Expression<Func<TEntity, IEnumerable<Ref<TKey, TRef>>>> selector)  {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RefsPath<TKey, TEntity, TRef>(selectorPath);
        }
    }
    
    public class RefPath<TKey, TEntity, TRef> : RefsPath<TKey, TEntity, TRef>
                                            where TEntity : class
                                            where TRef    : class 
    {
        internal RefPath(string path) : base (path) { }
        
        public static RefPath<TKey, TEntity, TRef> MemberRef(Expression<Func<TEntity, Ref<TKey, TRef>>> selector) {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RefPath<TKey, TEntity, TRef>(selectorPath);
        }
    }
}