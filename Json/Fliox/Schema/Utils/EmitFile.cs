﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Utils
{
    public sealed class EmitFile
    {
        /// contain all types of a file and their generated piece of code for each type
        public   readonly   List<EmitType>              emitTypes   = new List<EmitType>();
        /// contain all imports used by all types in a file
        public   readonly   Dictionary<TypeDef, Import> imports     = new Dictionary<TypeDef, Import>();
        /// the generated code used as file header. Typically all imports (using statements)
        public              string                      header;
        /// the generated code used as file footer. E.g. the closing brackets in case of JSON
        public              string                      footer;
        /// The path of the file
        private  readonly   string                      path;
        
        public   readonly   string                      @namespace;
        

        public  override    string                      ToString() => path;

        public EmitFile (string path, string @namespace) {
            this.path       = path;
            this.@namespace = @namespace;
        }
    }
    
    public readonly struct Import
    {
        public readonly TypeDef type;
        public readonly string  @namespace;
        
        public Import(TypeDef type, string  @namespace) {
            this.type       = type;
            this.@namespace = @namespace;
        }
    }
        
    public static class GeneratorExtension
    {
        // ReSharper disable UseMethodAny.2
        // ReSharper disable PossibleMultipleEnumeration
        public static int MaxLength<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector) {
            if (source.Count() == 0)
                return 0;
            return source.Max(selector); 
        }
    }
}