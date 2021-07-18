// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema
{
    public class Generator
    {
        public  readonly    ICollection<Type>                       rootTypes;
        public  readonly    string                                  folder;
        public  readonly    TypeStore                               typeStore;
        public  readonly    IReadOnlyDictionary<Type, TypeMapper>   typeMappers;
        
        
        public Generator (ICollection<Type> rootTypes, string folder, TypeStore typeStore) {
            this.rootTypes  = rootTypes;
            this.folder     = folder;
            this.typeStore  = typeStore;
            typeMappers     = typeStore.GetTypeMappers();
        }
    }
}