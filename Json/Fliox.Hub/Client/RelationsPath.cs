// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;

namespace Friflo.Json.Fliox.Hub.Client
{
    // --- Relation
    public class RelationsPath<TKey, T>   where T : class
    {
        public readonly string path;

        public override string ToString() => path;

        internal RelationsPath(string path) {
            this.path = path;
        }
        
        public static RelationsPath<TKey, T> MemberRefs(Expression<Func<T, IEnumerable<TKey>>> selector)  {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RelationsPath<TKey, T>(selectorPath);
        }
    }
    
    public sealed class RelationPath<TKey, T> : RelationsPath<TKey, T> where T : class
    {
        internal RelationPath(string path) : base (path) { }
        
        public static RelationPath<TKey, T> MemberRef(Expression<Func<T, TKey>> selector) {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RelationPath<TKey, T>(selectorPath);
        }
    }
}