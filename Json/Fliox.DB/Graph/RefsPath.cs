// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.DB.Graph.Internal;

namespace Friflo.Json.Fliox.DB.Graph
{
    public class RefsPath<TEntity, TRefKey, TRef>   where TEntity : class
                                                    where TRef    : class 
    {
        public readonly string path;

        public override string ToString() => path;

        internal RefsPath(string path) {
            this.path = path;
        }
        
        public static RefsPath<TEntity, TRefKey, TRef> MemberRefs(Expression<Func<TEntity, IEnumerable<Ref<TRefKey, TRef>>>> selector)  {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RefsPath<TEntity, TRefKey, TRef>(selectorPath);
        }
    }
    
    public class RefPath<TEntity, TRefKey, TRef> : RefsPath<TEntity, TRefKey, TRef>
                                            where TEntity : class
                                            where TRef    : class 
    {
        internal RefPath(string path) : base (path) { }
        
        public static RefPath<TEntity, TRefKey, TRef> MemberRef(Expression<Func<TEntity, Ref<TRefKey, TRef>>> selector) {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RefPath<TEntity, TRefKey, TRef>(selectorPath);
        }
    }
}