// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.Utils
{
    public class EmitFile
    {
        /// contain all types of a file and their generated piece of code for each type
        public   readonly   List<EmitType>      emitTypes   = new List<EmitType>();
        /// contain all imports used by all types in a file
        public   readonly   HashSet<TypeDef>    imports     = new HashSet<TypeDef>();
        /// the generated code used as file header. Typically all imports (using statements)
        public              string              header;
        /// the generated code used as file footer. E.g. the closing brackets in case of JSON
        public              string              footer;
        /// The path of the file
        private  readonly   string              path;
        
        public   readonly   string              package;
        

        public  override    string              ToString() => path;

        public EmitFile (string path, string package) {
            this.path       = path;
            this.package    = package;
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