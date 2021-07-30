// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema
{
    public class JsonTypeOptions
    {
        public  readonly    TypeSchema              schema;
        public              string                  fileExt;
        public              ICollection<string>     stripNamespaces;
        public              ICollection<TypeDef>    separateTypes;
        public              Func<TypeDef, string>   getPath;
        
        public JsonTypeOptions (TypeSchema schema) {
            this.schema     = schema;
        }
    }
    
    public class NativeTypeOptions
    {
        public  readonly    TypeStore               typeStore; 
        public  readonly    ICollection<Type>       rootTypes;
        public              string                  fileExt;
        public              ICollection<string>     stripNamespaces;
        public              ICollection<Type>       separateTypes;
        public              Func<TypeDef, string>   getPath;
        
        public NativeTypeOptions (TypeStore typeStore, ICollection<Type> rootTypes) {
            this.typeStore  = typeStore;
            this.rootTypes  = rootTypes;
        }
    }
}