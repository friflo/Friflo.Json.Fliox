// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Friflo.Json.Flow.Schema.Utils
{
    public class Package
    {
        /// contain all types of a namespace (package) and their generated piece of code for each type
        internal readonly   List<EmitType>              emitTypes   = new List<EmitType>();
        /// contain all imports used by all types in a package
        public   readonly   Dictionary<Type, Import>    imports     = new Dictionary<Type, Import>();
        /// the generated code used as package header. Typically all imports (using statements)
        public              string                      header;
        public              string                      footer;
    }
    
    public readonly struct Import
    {
        public readonly Type    type;
        
        public Import (Type type) {
            this.type = type;
        }
    }
    
        
    public static class GeneratorExtension
    {
        public static int MaxLength<TSource>(this ICollection<TSource> source, Func<TSource, int> selector) {
            if (source.Count == 0)
                return 0;
            return source.Max(selector); 
        }
    }
}