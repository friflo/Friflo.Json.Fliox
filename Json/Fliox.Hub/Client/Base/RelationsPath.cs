// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    // --- Relation
    public class RelationsPath<TRef>   where TRef : class
    {
        public readonly string path;

        public override string ToString() => path;

        internal RelationsPath(string path) {
            this.path = path;
        }
        
        public static RelationsPath<TRef> Create<TKey,T>(Expression<Func<T, IEnumerable<TKey>>> selector)  {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RelationsPath<TRef>(selectorPath);
        }
        
        public static RelationsPath<TRef> Create<TKey,T>(Expression<Func<T, IEnumerable<TKey?>>> selector) where TKey : struct {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RelationsPath<TRef>(selectorPath);
        }
    }
    
    public sealed class RelationPath<TRef> : RelationsPath<TRef> where TRef : class
    {
        internal RelationPath(string path) : base (path) { }
        
        public static RelationPath<TRef> Create<TKey,T>(Expression<Func<T, TKey>> selector) {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RelationPath<TRef>(selectorPath);
        }
        
        public static RelationPath<TRef> Create<TKey,T>(Expression<Func<T, TKey?>> selector) where TKey : struct {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RelationPath<TRef>(selectorPath);
        }
    }
}