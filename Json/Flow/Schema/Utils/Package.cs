// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.Utils
{
    public class Package
    {
        /// contain all types of a namespace (package) and their generated piece of code for each type
        public   readonly   List<EmitType>      emitTypes   = new List<EmitType>();
        /// contain all imports used by all types in a package
        public   readonly   HashSet<TypeDef>    imports     = new HashSet<TypeDef>();
        /// the generated code used as package header. Typically all imports (using statements)
        public              string              header;
        public              string              footer;
        private  readonly   string              name;

        public  override    string              ToString() => name;

        public Package (string name) {
            this.name = name;
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