// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Schema
{
    public class Generator
    {
        public readonly    ICollection<Type>    types;
        public readonly    string               folder;
        public readonly    TypeStore            typeStore;
        
        
        public Generator (ICollection<Type> types, string folder, TypeStore typeStore) {
            this.types      = types;
            this.folder     = folder;
            this.typeStore  = typeStore;
        }
    }
}